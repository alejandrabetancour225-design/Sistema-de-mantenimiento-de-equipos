using Backend.API.Models;

namespace Backend.API.DTOs;

public class ChangeStatusRequest
{
    public EquipmentStatus? Status { get; set; }
}