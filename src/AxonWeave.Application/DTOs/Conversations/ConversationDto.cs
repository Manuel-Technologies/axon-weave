using AxonWeave.Application.DTOs.Users;

namespace AxonWeave.Application.DTOs.Conversations;

public class ConversationDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required IReadOnlyCollection<UserDto> Participants { get; init; }
    public MessagePreviewDto? LastMessage { get; init; }
}
