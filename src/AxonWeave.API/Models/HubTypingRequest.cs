namespace AxonWeave.API.Models;

public class HubTypingRequest
{
    public Guid ConversationId { get; set; }
    public bool IsTyping { get; set; }
}
