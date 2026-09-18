using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Equipment> Equipments => Set<Equipment>();

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

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"), Name = Backend.API.Models.Roles.Administrador },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"), Name = Backend.API.Models.Roles.Tecnico },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"), Name = Backend.API.Models.Roles.Empleado },
            new Role { Id = Guid.Parse("a0000000-0000-0000-0000-000000000004"), Name = Backend.API.Models.Roles.Cliente });
    }
}