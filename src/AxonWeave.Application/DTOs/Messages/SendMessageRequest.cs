using System.ComponentModel.DataAnnotations;

namespace AxonWeave.Application.DTOs.Messages;

public class SendMessageRequest
{
    [Required]
    public Guid ConversationId { get; set; }
    [StringLength(20000)]
    public string EncryptedContent { get; set; } = string.Empty;
    [StringLength(2048)]
    public string? MediaUrl { get; set; }
    [StringLength(128)]
    public string? MediaContentType { get; set; }
}
