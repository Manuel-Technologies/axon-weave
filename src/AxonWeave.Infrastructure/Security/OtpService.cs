using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.Options;
using Microsoft.Extensions.Options;

namespace AxonWeave.Infrastructure.Security;

public class OtpService : IOtpService
{
    private readonly OtpOptions _options;
    private readonly Random _random = new();

    public OtpService(IOptions<OtpOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateCode() => _options.UseStaticOtp ? _options.StaticOtpCode : _random.Next(100000, 999999).ToString();

    public string HashCode(string code) => BCrypt.Net.BCrypt.HashPassword(code);

    public bool Verify(string code, string codeHash) => BCrypt.Net.BCrypt.Verify(code, codeHash);
}
