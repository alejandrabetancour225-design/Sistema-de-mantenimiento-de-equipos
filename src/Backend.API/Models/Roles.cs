namespace Backend.API.Models;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Tecnico = "Técnico";
    public const string Empleado = "Empleado";
    public const string Cliente = "Cliente";

    public static readonly string[] All = { Administrador, Tecnico, Empleado, Cliente };

    // Combinaciones para [Authorize(Roles = ...)].
    public const string AdminOrTechnician = Administrador + "," + Tecnico;
    public const string AdminOrEmployee = Administrador + "," + Empleado;

    // Personal interno: todos los roles menos Cliente.
    public const string Staff = Administrador + "," + Tecnico + "," + Empleado;

    // Cualquier rol válido. El Cliente solo llega a la consulta del historial de mantenimiento.
    public const string AnyRole = Administrador + "," + Tecnico + "," + Empleado + "," + Cliente;
}
