using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class CreateAssignmentRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [StringLength(1000)]
    public string? Observations { get; set; }
}
