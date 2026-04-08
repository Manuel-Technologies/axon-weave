namespace AxonWeave.Application.DTOs.Users;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Name { get; init; }
}
