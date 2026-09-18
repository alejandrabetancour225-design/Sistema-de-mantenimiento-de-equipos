namespace Backend.API.Models;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Tecnico = "Técnico";
    public const string Empleado = "Empleado";
    public const string Cliente = "Cliente";

    public static readonly string[] All = { Administrador, Tecnico, Empleado, Cliente };
}