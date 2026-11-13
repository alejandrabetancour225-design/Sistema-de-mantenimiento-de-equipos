namespace Backend.API.Models;

public enum MaintenanceStatus
{
    OPEN,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED,
    // Mantenimiento completado que se anuló por contener errores. Se conserva en el historial,
    // pero no cuenta en costos ni reportes; la corrección se registra como un mantenimiento nuevo.
    VOIDED
}
