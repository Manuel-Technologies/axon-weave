namespace AxonWeave.Application.DTOs.Messages;

public class MessageDto
{
    public required Guid Id { get; init; }
    public required Guid ConversationId { get; init; }
    public required Guid SenderId { get; init; }
    public required string EncryptedContent { get; init; }
    public string? MediaUrl { get; init; }
    public string? MediaContentType { get; init; }
    public required bool IsDeletedForEveryone { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
    public required IReadOnlyCollection<MessageDeliveryDto> Deliveries { get; init; }
}
