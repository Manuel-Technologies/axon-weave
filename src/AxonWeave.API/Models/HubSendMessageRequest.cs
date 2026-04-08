namespace AxonWeave.API.Models;

public class HubSendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string EncryptedContent { get; set; } = string.Empty;
}
