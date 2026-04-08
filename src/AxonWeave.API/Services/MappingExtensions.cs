using AxonWeave.Application.DTOs.Conversations;
using AxonWeave.Application.DTOs.Messages;
using AxonWeave.Application.DTOs.Users;
using AxonWeave.Domain.Entities;
using AxonWeave.Domain.Enums;

namespace AxonWeave.API.Services;

public static class MappingExtensions
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        PhoneNumber = user.PhoneNumber,
        Name = user.Name
    };

    public static MessageDto ToDto(this Message message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        EncryptedContent = message.EncryptedContent,
        MediaUrl = message.MediaUrl,
        MediaContentType = message.MediaContentType,
        IsDeletedForEveryone = message.IsDeletedForEveryone,
        CreatedAt = message.CreatedAt,
        ReadAt = message.ReadAt,
        Deliveries = message.Deliveries
            .OrderBy(x => x.UserId)
            .Select(x => new MessageDeliveryDto
            {
                UserId = x.UserId,
                Status = x.Status.ToString(),
                DeliveredAt = x.DeliveredAt,
                ReadAt = x.ReadAt
            })
            .ToList()
    };

    public static ConversationDto ToDto(this Conversation conversation) => new()
    {
        Id = conversation.Id,
        Title = conversation.Title,
        Type = conversation.Type == ConversationType.Group ? "group" : "direct",
        CreatedAt = conversation.CreatedAt,
        UpdatedAt = conversation.UpdatedAt,
        Participants = conversation.Participants.Select(x => x.User.ToDto()).ToList(),
        LastMessage = conversation.Messages
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new MessagePreviewDto
            {
                Id = x.Id,
                EncryptedContent = x.EncryptedContent,
                CreatedAt = x.CreatedAt,
                SenderId = x.SenderId
            })
            .FirstOrDefault()
    };
}
