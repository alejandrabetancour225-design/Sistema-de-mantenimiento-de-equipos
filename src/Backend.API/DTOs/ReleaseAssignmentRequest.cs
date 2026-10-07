using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class ReleaseAssignmentRequest
{
    [StringLength(1000)]
    public string? Observations { get; set; }
}
