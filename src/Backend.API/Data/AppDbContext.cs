using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

    // Nombres de los índices únicos parciales que evitan duplicados por peticiones simultáneas.
    public const string ActiveAssignmentPerEquipmentIndex = "IX_Assignments_EquipmentId_Active";
    public const string OpenIncidentPerEquipmentIndex = "IX_Incidents_EquipmentId_Open";
    public const string OpenMaintenancePerEquipmentIndex = "IX_Maintenances_EquipmentId_Open";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.EmailHash).IsUnique();
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasIndex(e => e.InternalCode).IsUnique();
            entity.HasIndex(e => e.SerialNumber).IsUnique();
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
            entity.HasIndex(s => s.Name).IsUnique();
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
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000004"), Name = Backend.API.Models.Roles.Cliente }
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
    }
}
