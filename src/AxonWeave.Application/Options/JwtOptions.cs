namespace AxonWeave.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "axon-weave";
    public string Audience { get; set; } = "axon-weave-client";
    public string SecretKey { get; set; } = "change-this-super-secret-key-for-production";
    public int ExpiryMinutes { get; set; } = 1440;
}
