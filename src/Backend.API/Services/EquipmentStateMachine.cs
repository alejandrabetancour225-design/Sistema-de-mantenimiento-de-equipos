using Backend.API.Data;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

// Acciones del sistema que pueden cambiar el estado de un equipo.
public enum EquipmentTransition
{
    Assign,
    Release,
    OpenMaintenance,
    CloseMaintenance,
    CancelMaintenance,
    Manual,
    Decommission,
    Return
}

// R4 – Regla maestra de transiciones de estado (HU-06).
// Es el ÚNICO lugar del sistema donde se decide y se escribe el estado de un equipo.
//
//   Asignar               AVAILABLE                         -> IN_USE
//   Liberar               IN_USE                            -> AVAILABLE   (otros estados no cambian)
//   Abrir mantenimiento   AVAILABLE | IN_USE | OUT_OF_SERVICE -> UNDER_MAINTENANCE
//   Cerrar mantenimiento  UNDER_MAINTENANCE                 -> estado de reposo | OUT_OF_SERVICE
//   Cancelar/eliminar     UNDER_MAINTENANCE                 -> estado de reposo
//   Manual                AVAILABLE | IN_USE                -> OUT_OF_SERVICE
//                         OUT_OF_SERVICE                    -> estado de reposo
//   Dar de baja (Admin)   AVAILABLE | OUT_OF_SERVICE        -> DECOMMISSIONED (terminal, solo propios)
//   Devolver (alquilado)  AVAILABLE | OUT_OF_SERVICE        -> RETURNED (terminal)
//
// "Estado de reposo" = IN_USE si el equipo tiene una asignación activa, AVAILABLE si no.
public static class EquipmentStateMachine
{
    public static bool IsAllowed(EquipmentStatus from, EquipmentStatus to, EquipmentTransition trigger)
    {
        if (from is EquipmentStatus.DECOMMISSIONED or EquipmentStatus.RETURNED)
        {
            return false;
        }

        return trigger switch
        {
            EquipmentTransition.Assign =>
                from == EquipmentStatus.AVAILABLE && to == EquipmentStatus.IN_USE,

            EquipmentTransition.Release =>
                from == EquipmentStatus.IN_USE && to == EquipmentStatus.AVAILABLE,

            EquipmentTransition.OpenMaintenance =>
                from is EquipmentStatus.AVAILABLE or EquipmentStatus.IN_USE or EquipmentStatus.OUT_OF_SERVICE
                && to == EquipmentStatus.UNDER_MAINTENANCE,

            EquipmentTransition.CloseMaintenance =>
                from == EquipmentStatus.UNDER_MAINTENANCE
                && to is EquipmentStatus.AVAILABLE or EquipmentStatus.IN_USE or EquipmentStatus.OUT_OF_SERVICE,

            EquipmentTransition.CancelMaintenance =>
                from == EquipmentStatus.UNDER_MAINTENANCE
                && to is EquipmentStatus.AVAILABLE or EquipmentStatus.IN_USE,

            EquipmentTransition.Manual =>
                (from is EquipmentStatus.AVAILABLE or EquipmentStatus.IN_USE && to == EquipmentStatus.OUT_OF_SERVICE)
                || (from == EquipmentStatus.OUT_OF_SERVICE && to is EquipmentStatus.AVAILABLE or EquipmentStatus.IN_USE),

            EquipmentTransition.Decommission =>
                from is EquipmentStatus.AVAILABLE or EquipmentStatus.OUT_OF_SERVICE
                && to == EquipmentStatus.DECOMMISSIONED,

            EquipmentTransition.Return =>
                from is EquipmentStatus.AVAILABLE or EquipmentStatus.OUT_OF_SERVICE
                && to == EquipmentStatus.RETURNED,

            _ => false
        };
    }

    // Estados que se pueden elegir al registrar un equipo nuevo (no tiene asignación ni mantenimiento).
    public static bool IsValidInitialStatus(EquipmentStatus status)
        => status is EquipmentStatus.AVAILABLE or EquipmentStatus.OUT_OF_SERVICE;

    public static async Task<EquipmentStatus> GetRestingStatusAsync(AppDbContext context, Guid equipmentId)
    {
        var hasActiveAssignment = await context.Assignments
            .AnyAsync(a => a.EquipmentId == equipmentId && a.Status == AssignmentStatus.ACTIVE);

        return hasActiveAssignment ? EquipmentStatus.IN_USE : EquipmentStatus.AVAILABLE;
    }

    // Aplica la transición si está permitida. Devuelve false (sin tocar el equipo) si no lo está.
    public static bool TryApply(Equipment equipment, EquipmentStatus to, EquipmentTransition trigger)
    {
        if (equipment.Status == to)
        {
            return true;
        }

        if (!IsAllowed(equipment.Status, to, trigger))
        {
            return false;
        }

        equipment.Status = to;
        equipment.UpdatedAt = DateTime.UtcNow;
        return true;
    }
}
