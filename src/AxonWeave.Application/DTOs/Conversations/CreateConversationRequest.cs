namespace AxonWeave.Application.DTOs.Conversations;

public class CreateConversationRequest
{
    public string? Title { get; set; }
    public bool IsGroup { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
}
