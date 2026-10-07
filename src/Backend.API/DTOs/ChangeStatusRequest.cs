using System.ComponentModel.DataAnnotations;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class ChangeStatusRequest
{
    [EnumDataType(typeof(EquipmentStatus))]
    public EquipmentStatus? Status { get; set; }
}
