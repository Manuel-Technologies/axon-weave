namespace AxonWeave.Application.DTOs.Messages;

public class MessageDeliveryDto
{
    public required Guid UserId { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
}
