using System.ComponentModel.DataAnnotations;

namespace AxonWeave.Application.DTOs.Conversations;

public class CreateConversationRequest
{
    [StringLength(160)]
    public string? Title { get; set; }
    public bool IsGroup { get; set; }
    [MinLength(1)]
    public List<Guid> ParticipantIds { get; set; } = new();
}
