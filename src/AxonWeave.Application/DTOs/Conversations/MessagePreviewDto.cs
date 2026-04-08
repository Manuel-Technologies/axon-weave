namespace AxonWeave.Application.DTOs.Conversations;

public class MessagePreviewDto
{
    public required Guid Id { get; init; }
    public required string EncryptedContent { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required Guid SenderId { get; init; }
}
