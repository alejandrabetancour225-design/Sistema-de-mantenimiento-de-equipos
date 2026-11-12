namespace Backend.API.Models;

public static class Roles
{
    // Administra la plataforma: crea organizaciones y les asigna plan. No ve datos de las empresas.
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrador = "Administrador";
    public const string Tecnico = "Técnico";
    public const string Empleado = "Empleado";
    // Antes "Cliente": consulta dashboard gerencial, reportes e historial.
    public const string Gerente = "Gerente";

    // Roles que se pueden asignar dentro de una organización.
    public static readonly string[] OrganizationRoles = { Administrador, Tecnico, Empleado, Gerente };
    public static readonly string[] All = { SuperAdmin, Administrador, Tecnico, Empleado, Gerente };

    // Combinaciones para [Authorize(Roles = ...)].
    public const string AdminOrTechnician = Administrador + "," + Tecnico;
    public const string AdminOrEmployee = Administrador + "," + Empleado;
    public const string AdminOrManager = Administrador + "," + Gerente;

    // Personal interno: todos los roles de la empresa menos Gerente.
    public const string Staff = Administrador + "," + Tecnico + "," + Empleado;

    // Cualquier rol de una organización (el SuperAdmin no entra a los datos de las empresas).
    public const string AnyRole = Administrador + "," + Tecnico + "," + Empleado + "," + Gerente;
}
