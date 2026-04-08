using AxonWeave.Domain.Enums;

namespace AxonWeave.Domain.Entities;

public class MessageDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public MessageDeliveryStatus Status { get; set; } = MessageDeliveryStatus.Pending;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
