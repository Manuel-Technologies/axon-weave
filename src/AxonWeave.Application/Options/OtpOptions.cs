namespace AxonWeave.Application.Options;

public class OtpOptions
{
    public const string SectionName = "Otp";
    public int ExpiryMinutes { get; set; } = 5;
    public bool UseStaticOtp { get; set; }
    public string StaticOtpCode { get; set; } = "123456";
}
