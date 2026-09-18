using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class UpdateUserRequest
{
    public bool? Active { get; set; }
    public string? Role { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }
}