namespace AxonWeave.Application.DTOs.Auth;

public class RegisterResponse
{
    public required string PhoneNumber { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset OtpExpiresAt { get; init; }
    public string? DevelopmentOtp { get; init; }

    // public RegisterResponse(string phoneNumber, string name, DateTimeOffset otpExpiresAt, string? developmentOtp = null)
    // {
    //     PhoneNumber = phoneNumber;
    //     Name = name;
    //     OtpExpiresAt = otpExpiresAt;
    //     DevelopmentOtp = developmentOtp;
    // }
}
