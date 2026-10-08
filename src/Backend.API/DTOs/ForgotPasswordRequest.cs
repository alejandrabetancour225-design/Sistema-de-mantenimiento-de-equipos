using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
