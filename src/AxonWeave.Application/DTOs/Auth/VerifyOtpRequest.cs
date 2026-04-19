using System.ComponentModel.DataAnnotations;

namespace AxonWeave.Application.DTOs.Auth;

public class VerifyOtpRequest
{
    [Required]
    [StringLength(32, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(12, MinimumLength = 4)]
    public string Code { get; set; } = string.Empty;
}
