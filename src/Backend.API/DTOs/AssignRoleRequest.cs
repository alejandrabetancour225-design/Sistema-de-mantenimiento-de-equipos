using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class AssignRoleRequest
{
    [Required]
    [StringLength(50)]
    public string Role { get; set; } = string.Empty;
}
