namespace Backend.API.Models;

public enum EquipmentStatus
{
    AVAILABLE,
    IN_USE,
    UNDER_MAINTENANCE,
    OUT_OF_SERVICE,
    DECOMMISSIONED,
    // Equipo alquilado devuelto al arrendador (terminal, conserva su historial).
    RETURNED
}