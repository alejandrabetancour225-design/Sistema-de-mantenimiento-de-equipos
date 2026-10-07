namespace Backend.API.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string EmailHash { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public int FailedAttempts { get; set; }

    // Bloqueo temporal tras varios intentos fallidos (no desactiva la cuenta).
    public DateTime? LockoutEnd { get; set; }

    // Cambia cuando se modifica el rol, el estado o la contraseña; invalida los tokens emitidos antes.
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid? RoleId { get; set; }
    public Role? Role { get; set; }

    public ICollection<Assignment>? Assignments { get; set; }
    public ICollection<Incident>? Incidents { get; set; }
    public ICollection<Maintenance>? Maintenances { get; set; }
}