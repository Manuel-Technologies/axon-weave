namespace AxonWeave.Domain.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string EncryptedContent { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaContentType { get; set; }
    public bool IsDeletedForEveryone { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<MessageDelivery> Deliveries { get; set; } = new List<MessageDelivery>();
}
