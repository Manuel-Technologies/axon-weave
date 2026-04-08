using AxonWeave.Domain.Entities;

namespace AxonWeave.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
