using AxonWeave.Application.DTOs.Users;

namespace AxonWeave.Application.DTOs.Auth;

public class AuthResponse
{
    public required string Token { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required UserDto User { get; init; }

    // public AuthResponse(string token, DateTimeOffset expiresAt, UserDto user)
    // {
    //     Token = token;
    //     ExpiresAt = expiresAt;
    //     User = user;
        
    // }
}
