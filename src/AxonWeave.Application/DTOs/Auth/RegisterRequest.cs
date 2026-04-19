using System.ComponentModel.DataAnnotations;

namespace AxonWeave.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [StringLength(32, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}
