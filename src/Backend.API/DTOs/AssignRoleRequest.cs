using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class AssignRoleRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
}