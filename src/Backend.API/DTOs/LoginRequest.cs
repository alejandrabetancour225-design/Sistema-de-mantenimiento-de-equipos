using System.ComponentModel.DataAnnotations;
using Backend.API.Validation;

namespace Backend.API.DTOs;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(StrongPasswordAttribute.MaxLength)]
    public string Password { get; set; } = string.Empty;
}
