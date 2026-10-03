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
        });

        modelBuilder.Entity<Maintenance>(entity =>
        {
            entity.HasOne(m => m.Equipment)
                .WithMany(e => e.Maintenances!)
                .HasForeignKey(m => m.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Technician)
                .WithMany(u => u.Maintenances!)
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

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"), Name = Backend.API.Models.Roles.Administrador },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"), Name = Backend.API.Models.Roles.Tecnico },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"), Name = Backend.API.Models.Roles.Empleado },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000004"), Name = Backend.API.Models.Roles.Cliente });
    }
}