namespace AxonWeave.Application.Common.Interfaces;

public interface IOtpService
{
    string GenerateCode();
    string HashCode(string code);
    bool Verify(string code, string codeHash);
}
