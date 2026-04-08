namespace AxonWeave.Application.DTOs.Auth;

public class VerifyOtpRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
