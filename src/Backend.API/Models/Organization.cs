namespace Backend.API.Models;

// Plan de membresía (Q25). Lo asigna a mano el SuperAdmin; no hay pagos ni cobros.
public enum MembershipPlan
{
    // Inventario, asignaciones, mantenimientos, alquiler, calendario, reportes y diagnóstico con IA.
    BASICO,
    // Todo lo anterior + conexión a bases de datos de terceros, minería de datos,
    // reportes generados por IA e información de mercado.
    INTEGRACIONES
}

// Empresa cliente del sistema (multiempresa). Cada una ve solo sus propios datos.
public class Organization
{
    // Organización a la que se migraron los datos existentes antes del Sprint 6.
    public static readonly Guid DefaultId = Guid.Parse("0e000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MembershipPlan Plan { get; set; } = MembershipPlan.BASICO;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Entidades que pertenecen a una organización. El DbContext filtra por la organización
// del usuario autenticado y completa OrganizationId al crear registros.
public interface IOrganizationScoped
{
    Guid OrganizationId { get; set; }
}
