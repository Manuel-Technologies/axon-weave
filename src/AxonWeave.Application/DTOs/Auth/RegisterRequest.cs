namespace AxonWeave.Application.DTOs.Auth;

public class RegisterRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
