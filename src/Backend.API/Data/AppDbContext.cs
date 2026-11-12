using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenant = null) : base(options)
    {
        _tenant = tenant;
    }

    // Organización de la petición (null = sin filtro: arranque, login, tareas programadas).
    // Los filtros globales de consulta la leen en cada consulta (multiempresa, Q5).
    public Guid? CurrentOrganizationId => _tenant?.OrganizationId;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Lessor> Lessors => Set<Lessor>();
    public DbSet<RentalContract> RentalContracts => Set<RentalContract>();
    public DbSet<RentalRate> RentalRates => Set<RentalRate>();
    public DbSet<LessorClaim> LessorClaims => Set<LessorClaim>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<EquipmentComponent> EquipmentComponents => Set<EquipmentComponent>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Maintenance> Maintenances => Set<Maintenance>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<MaintenanceSparePart> MaintenanceSpareParts => Set<MaintenanceSparePart>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<ErrorKnowledge> ErrorKnowledges => Set<ErrorKnowledge>();
    public DbSet<AIDiagnosis> AIDiagnoses => Set<AIDiagnosis>();
    public DbSet<FailurePrediction> FailurePredictions => Set<FailurePrediction>();
    public DbSet<AIFeedback> AIFeedbacks => Set<AIFeedback>();
    public DbSet<SlangTerm> SlangTerms => Set<SlangTerm>();

    // Nombres de los índices únicos parciales que evitan duplicados por peticiones simultáneas.
    public const string ActiveAssignmentPerEquipmentIndex = "IX_Assignments_EquipmentId_Active";
    public const string OpenIncidentPerEquipmentIndex = "IX_Incidents_EquipmentId_Open";
    public const string OpenMaintenancePerEquipmentIndex = "IX_Maintenances_EquipmentId_Open";

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignOrganization();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AssignOrganization();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Los registros nuevos heredan la organización del usuario. Si no hay organización
    // (código de arranque o SuperAdmin) debe venir indicada explícitamente.
    private void AssignOrganization()
    {
        foreach (var entry in ChangeTracker.Entries<IOrganizationScoped>().Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity.OrganizationId != Guid.Empty)
            {
                continue;
            }

            if (CurrentOrganizationId is { } org && org != Guid.Empty)
            {
                entry.Entity.OrganizationId = org;
            }
            else
            {
                throw new InvalidOperationException(
                    $"No se pudo determinar la organización para el nuevo {entry.Metadata.ClrType.Name}.");
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.Property(o => o.Name).HasMaxLength(150);
            entity.HasIndex(o => o.Name).IsUnique();
            entity.HasData(new Organization
            {
                Id = Organization.DefaultId,
                Name = "Organización por defecto",
                Plan = MembershipPlan.INTEGRACIONES,
                Active = true,
                CreatedAt = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        // Multiempresa: cada consulta solo ve los datos de la organización del usuario.
        modelBuilder.Entity<Equipment>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<Assignment>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<Incident>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<Maintenance>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<EquipmentComponent>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<SparePart>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<ErrorKnowledge>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<AIDiagnosis>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<FailurePrediction>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<AIFeedback>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<Lessor>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<RentalContract>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<LessorClaim>().HasQueryFilter(e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<User>().HasQueryFilter(u => CurrentOrganizationId == null || u.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(a => CurrentOrganizationId == null || a.OrganizationId == CurrentOrganizationId);
        // Glosario: las expresiones base (sin organización) las ven todas las empresas.
        modelBuilder.Entity<SlangTerm>().HasQueryFilter(t => CurrentOrganizationId == null || t.OrganizationId == null || t.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.EmailHash).IsUnique();
            entity.HasIndex(u => u.OrganizationId);
            entity.HasOne<Organization>().WithMany().HasForeignKey(u => u.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            // Únicos dentro de cada organización (dos empresas pueden usar el mismo código).
            entity.HasIndex(e => new { e.OrganizationId, e.InternalCode }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.SerialNumber }).IsUnique();
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RentalContract).WithMany(c => c.Equipment!).HasForeignKey(e => e.RentalContractId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.RentalContractId);
            entity.Property(e => e.Characteristics).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasOne(a => a.Equipment)
                .WithMany(e => e.Assignments!)
                .HasForeignKey(a => a.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.User)
                .WithMany(u => u.Assignments!)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(a => a.EquipmentId);
            entity.HasIndex(a => a.UserId);

            // Un equipo solo puede tener una asignación ACTIVE (Status = 0).
            entity.HasIndex(a => a.EquipmentId, ActiveAssignmentPerEquipmentIndex)
                .IsUnique()
                .HasFilter("\"Status\" = 0");
        });

        modelBuilder.Entity<EquipmentComponent>(entity =>
        {
            entity.HasOne(c => c.Equipment)
                .WithMany(e => e.Components!)
                .HasForeignKey(c => c.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(c => c.EquipmentId);
            entity.Property(c => c.Specifications).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasOne(i => i.Equipment)
                .WithMany(e => e.Incidents!)
                .HasForeignKey(i => i.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.Reporter)
                .WithMany(u => u.Incidents!)
                .HasForeignKey(i => i.ReportedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(i => i.EquipmentId);
            entity.HasIndex(i => i.ReportedBy);

            // Un equipo solo puede tener un incidente OPEN (0) o IN_PROGRESS (1).
            entity.HasIndex(i => i.EquipmentId, OpenIncidentPerEquipmentIndex)
                .IsUnique()
                .HasFilter("\"Status\" IN (0, 1)");
        });

        modelBuilder.Entity<Maintenance>(entity =>
        {
            entity.HasOne(m => m.Equipment)
                .WithMany(e => e.Maintenances!)
                .HasForeignKey(m => m.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Technician)
                .WithMany(u => u.MaintenancesAsTechnician!)
                .HasForeignKey(m => m.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Incident)
                .WithOne(i => i.Maintenance)
                .HasForeignKey<Maintenance>(m => m.IncidentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(m => m.LaborCost).HasPrecision(18, 2);
            entity.Property(m => m.OtherCosts).HasPrecision(18, 2);
            entity.HasIndex(m => m.EquipmentId);
            entity.HasIndex(m => m.IncidentId).IsUnique();
            entity.HasIndex(m => m.TechnicianId);
            entity.HasIndex(m => m.Status);
            entity.HasIndex(m => m.NextMaintenanceDate);

            // Un equipo solo puede tener un mantenimiento OPEN (0) o IN_PROGRESS (1).
            entity.HasIndex(m => m.EquipmentId, OpenMaintenancePerEquipmentIndex)
                .IsUnique()
                .HasFilter("\"Status\" IN (0, 1)");
        });

        modelBuilder.Entity<SparePart>(entity =>
        {
            entity.Property(s => s.UnitCost).HasPrecision(18, 2);
            entity.HasIndex(s => new { s.OrganizationId, s.Name }).IsUnique();
        });

        modelBuilder.Entity<MaintenanceSparePart>(entity =>
        {
            entity.HasOne(ms => ms.Maintenance)
                .WithMany(m => m.SpareParts!)
                .HasForeignKey(ms => ms.MaintenanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ms => ms.SparePart)
                .WithMany(s => s.Usages!)
                .HasForeignKey(ms => ms.SparePartId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(ms => ms.UnitCostAtUse).HasPrecision(18, 2);
            entity.HasIndex(ms => new { ms.MaintenanceId, ms.SparePartId }).IsUnique();
            entity.HasIndex(ms => ms.SparePartId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"), Name = Backend.API.Models.Roles.Administrador },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"), Name = Backend.API.Models.Roles.Tecnico },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"), Name = Backend.API.Models.Roles.Empleado },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000004"), Name = Backend.API.Models.Roles.Gerente },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000005"), Name = Backend.API.Models.Roles.SuperAdmin }
        );

        modelBuilder.Entity<PasswordResetCode>(entity =>
        {
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.ExpiresAt);
        });

        modelBuilder.Entity<Lessor>(entity =>
        {
            entity.Property(l => l.Name).HasMaxLength(150);
            entity.Property(l => l.ContactEmail).HasMaxLength(254);
            entity.HasIndex(l => new { l.OrganizationId, l.Name }).IsUnique();
        });

        modelBuilder.Entity<RentalContract>(entity =>
        {
            entity.Property(c => c.ContractNumber).HasMaxLength(60);
            entity.HasIndex(c => new { c.OrganizationId, c.ContractNumber }).IsUnique();
            entity.HasIndex(c => c.EndDate);
            entity.HasOne(c => c.Lessor).WithMany(l => l.Contracts!).HasForeignKey(c => c.LessorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RentalRate>(entity =>
        {
            entity.Property(r => r.EquipmentType).HasMaxLength(100);
            entity.Property(r => r.MonthlyRate).HasPrecision(18, 2);
            entity.HasIndex(r => new { r.ContractId, r.EquipmentType }).IsUnique();
            entity.HasOne(r => r.Contract).WithMany(c => c.Rates!).HasForeignKey(r => r.ContractId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessorClaim>(entity =>
        {
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.LessorResponse).HasMaxLength(2000);
            entity.HasIndex(c => c.EquipmentId);
            entity.HasIndex(c => c.Status);
            entity.HasOne(c => c.Equipment).WithMany().HasForeignKey(c => c.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Contract).WithMany().HasForeignKey(c => c.ContractId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SlangTerm>(entity =>
        {
            entity.Property(t => t.Expression).HasMaxLength(120);
            entity.Property(t => t.Meaning).HasMaxLength(400);
            entity.Property(t => t.Category).HasMaxLength(40);
            entity.HasIndex(t => new { t.OrganizationId, t.Expression }).IsUnique();
        });

        modelBuilder.Entity<ErrorKnowledge>(entity =>
        {
            entity.HasIndex(e => e.Symptom);
            entity.HasIndex(e => e.ComponentType);
            entity.HasIndex(e => e.Active);
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AIDiagnosis>(entity =>
        {
            entity.HasOne(d => d.Equipment)
                .WithMany()
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Incident)
                .WithMany()
                .HasForeignKey(d => d.IncidentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(d => d.EquipmentId);
            entity.HasIndex(d => d.CreatedAt);
            entity.HasIndex(d => d.Status);
            entity.Property(d => d.RankedCausesJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<FailurePrediction>(entity =>
        {
            entity.HasOne(p => p.Equipment)
                .WithMany()
                .HasForeignKey(p => p.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.EquipmentId);
            entity.HasIndex(p => p.RiskScore);
            entity.HasIndex(p => p.PredictedAt);
            entity.HasIndex(p => p.Acknowledged);
        });

        modelBuilder.Entity<AIFeedback>(entity =>
        {
            entity.HasOne(f => f.Diagnosis)
                .WithMany()
                .HasForeignKey(f => f.DiagnosisId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Technician)
                .WithMany()
                .HasForeignKey(f => f.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(f => f.DiagnosisId);
            entity.HasIndex(f => f.TechnicianId);
            entity.HasIndex(f => f.CreatedAt);
            entity.HasIndex(f => f.IsCorrect);
        });
    }
}
