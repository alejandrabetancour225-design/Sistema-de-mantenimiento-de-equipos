using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Data;

// Datos de demostración (reemplaza a seed.sh, que solo funcionaba en bash).
// Se activa con Seed:Demo=true (variable SEED_DEMO=true en el .env / iniciar.sh).
// Es idempotente: si el equipo DEMO-001 ya existe, no hace nada. Así varios compañeros
// pueden arrancar contra la misma base de Neon sin duplicar datos.
public static class DemoDataSeeder
{
    public const string DemoPassword = "DemoSprint6x2026";

    private static readonly (string Email, string Name, string Role)[] DemoUsers =
    {
        ("empleado1@demo.local", "Laura Gómez (demo)", Roles.Empleado),
        ("empleado2@demo.local", "Andrés Rojas (demo)", Roles.Empleado),
        ("tecnico1@demo.local", "Camilo Herrera (demo)", Roles.Tecnico),
        ("gerente1@demo.local", "Paola Martínez (demo)", Roles.Cliente)
    };

    public static async Task SeedAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (await context.Equipments.AnyAsync(e => e.InternalCode == "DEMO-001", ct))
        {
            logger.LogInformation("Datos de demostración ya presentes: no se vuelven a crear.");
            return;
        }

        var encryption = services.GetRequiredService<IEncryptionService>();
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var roles = await context.Roles.ToDictionaryAsync(r => r.Name, r => r.Id, ct);
        var now = DateTime.UtcNow;
        var today = LocalClock.Today;

        // --- Usuarios ---
        var users = new Dictionary<string, Guid>();
        foreach (var (email, name, role) in DemoUsers)
        {
            var normalized = encryption.Normalize(email);
            var candidates = encryption.ComputeLookupCandidates(normalized);
            var existing = await context.Users.FirstOrDefaultAsync(u => candidates.Contains(u.EmailHash), ct);
            if (existing is not null)
            {
                users[email] = existing.Id;
                continue;
            }

            var id = Guid.NewGuid();
            context.Users.Add(new User
            {
                Id = id,
                Email = encryption.Encrypt(normalized, EncryptionContexts.UserEmail(id)),
                EmailHash = encryption.ComputeLookup(normalized),
                PasswordHash = hasher.Hash(DemoPassword),
                FullName = name,
                Phone = string.Empty,
                Active = true,
                SecurityStamp = Guid.NewGuid(),
                CreatedAt = now,
                UpdatedAt = now,
                RoleId = roles[role]
            });
            users[email] = id;
        }

        // --- Equipos ---
        Equipment NewEquipment(string code, string type, string brand, string model, string location,
            int ageDays, decimal price, EquipmentStatus status = EquipmentStatus.AVAILABLE) => new()
        {
            Id = Guid.NewGuid(),
            InternalCode = code,
            SerialNumber = $"SN-{code}",
            Type = type,
            Brand = brand,
            Model = model,
            Location = location,
            OperatingSystem = type == "Portátil" || type == "Escritorio" ? "Windows 11 Pro" : null,
            AcquisitionDate = today.AddDays(-ageDays),
            WarrantyUntil = today.AddDays(-ageDays).AddYears(2),
            AcquisitionPrice = price,
            Characteristics = type == "Portátil" ? "{\"RAM\":\"16 GB\",\"Disco\":\"SSD 512 GB\"}" : null,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };

        var eq = new[]
        {
            NewEquipment("DEMO-001", "Portátil", "Lenovo", "ThinkPad T14", "Bogotá - Piso 3", 400, 4_200_000m),
            NewEquipment("DEMO-002", "Portátil", "Dell", "Latitude 5420", "Bogotá - Piso 3", 900, 3_800_000m),
            NewEquipment("DEMO-003", "Escritorio", "HP", "ProDesk 400", "Medellín - Recepción", 1500, 2_900_000m),
            NewEquipment("DEMO-004", "Impresora", "Epson", "EcoTank L3250", "Bogotá - Piso 2", 700, 900_000m,
                EquipmentStatus.OUT_OF_SERVICE),
            NewEquipment("DEMO-005", "Monitor", "Samsung", "S24R350", "Medellín - Sala de juntas", 300, 650_000m),
            NewEquipment("DEMO-006", "Portátil", "Acer", "Aspire 5", "Bodega", 2200, 2_100_000m,
                EquipmentStatus.DECOMMISSIONED),
            NewEquipment("DEMO-007", "Portátil", "Asus", "VivoBook 15", "Cali - Ventas", 200, 2_600_000m),
            NewEquipment("DEMO-008", "Escritorio", "Lenovo", "ThinkCentre M70", "Cali - Ventas", 1100, 3_100_000m)
        };
        context.Equipments.AddRange(eq);

        // --- Asignaciones (R4: el equipo asignado queda En uso) ---
        void Assign(Equipment equipment, string email)
        {
            context.Assignments.Add(new Assignment
            {
                Id = Guid.NewGuid(),
                EquipmentId = equipment.Id,
                UserId = users[email],
                AssignedAt = now.AddDays(-30),
                Status = AssignmentStatus.ACTIVE,
                Observations = "Asignación de demostración"
            });
            equipment.Status = EquipmentStatus.IN_USE;
        }

        Assign(eq[0], "empleado1@demo.local");
        Assign(eq[1], "empleado1@demo.local");
        Assign(eq[6], "empleado2@demo.local");

        // --- Incidencias abiertas ---
        context.Incidents.AddRange(
            new Incident
            {
                Id = Guid.NewGuid(), EquipmentId = eq[1].Id, ReportedBy = users["empleado1@demo.local"],
                Description = "El portátil se pega a cada rato y se pone muy caliente", ReportedAt = now.AddDays(-5)
            },
            new Incident
            {
                Id = Guid.NewGuid(), EquipmentId = eq[6].Id, ReportedBy = users["empleado2@demo.local"],
                Description = "La pantalla parpadea cuando lo muevo", ReportedAt = now.AddHours(-6)
            });

        // --- Repuestos ---
        var ram = await context.SpareParts.FirstOrDefaultAsync(s => s.Name == "Memoria RAM DDR4 16 GB", ct);
        if (ram is null)
        {
            ram = new SparePart
            {
                Id = Guid.NewGuid(), Name = "Memoria RAM DDR4 16 GB", Description = "Kingston KVR32S22S8/16",
                UnitCost = 180_000m, CreatedAt = now, UpdatedAt = now
            };
            context.SpareParts.Add(ram);
        }

        if (!await context.SpareParts.AnyAsync(s => s.Name == "Disco SSD 512 GB", ct))
        {
            context.SpareParts.Add(new SparePart
            {
                Id = Guid.NewGuid(), Name = "Disco SSD 512 GB", Description = "Samsung 870 EVO",
                UnitCost = 260_000m, CreatedAt = now, UpdatedAt = now
            });
        }

        // --- Mantenimientos completados con próxima fecha (alimentan "próximos mantenimientos") ---
        Maintenance Completed(Equipment equipment, MaintenanceType type, int daysAgo, int nextInDays, string work)
            => new()
            {
                Id = Guid.NewGuid(),
                EquipmentId = equipment.Id,
                TechnicianId = users["tecnico1@demo.local"],
                Type = type,
                Status = MaintenanceStatus.COMPLETED,
                ReportedProblem = type == MaintenanceType.PREVENTIVE ? "Mantenimiento preventivo programado" : "Falla reportada",
                WorkDone = work,
                LaborCost = 80_000m,
                OtherCosts = 0m,
                NextMaintenanceDate = today.AddDays(nextInDays),
                Observations = "Dato de demostración",
                StartedAt = now.AddDays(-daysAgo),
                CompletedAt = now.AddDays(-daysAgo).AddHours(3),
                CreatedAt = now,
                UpdatedAt = now
            };

        var withRam = Completed(eq[0], MaintenanceType.CORRECTIVE, 60, 10, "Se cambió la memoria RAM defectuosa");
        context.Maintenances.AddRange(
            withRam,
            Completed(eq[2], MaintenanceType.PREVENTIVE, 170, -3, "Limpieza interna y cambio de pasta térmica"),
            Completed(eq[4], MaintenanceType.PREVENTIVE, 90, 25, "Revisión general"));
        context.MaintenanceSpareParts.Add(new MaintenanceSparePart
        {
            Id = Guid.NewGuid(), MaintenanceId = withRam.Id, SparePartId = ram.Id, Quantity = 1, UnitCostAtUse = ram.UnitCost
        });

        await context.SaveChangesAsync(ct);
        logger.LogWarning(
            "Datos de demostración creados: 8 equipos DEMO-00x y usuarios *@demo.local (contraseña {Password}).",
            DemoPassword);
    }
}
