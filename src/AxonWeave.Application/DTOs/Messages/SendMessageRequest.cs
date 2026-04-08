namespace AxonWeave.Application.DTOs.Messages;

public class SendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string EncryptedContent { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaContentType { get; set; }
}
