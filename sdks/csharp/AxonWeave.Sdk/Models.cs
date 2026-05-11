namespace AxonWeave.Sdk;

public record ApiResponse<T>(T Data);
public record RegisterRequest(string PhoneNumber, string Name);
public record RegisterResponse(string PhoneNumber, string Name, DateTimeOffset OtpExpiresAt, string? DevelopmentOtp);
public record VerifyOtpRequest(string PhoneNumber, string Code);
public record UserDto(Guid Id, string PhoneNumber, string Name);
public record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);
public record CreateConversationRequest(string? Title, bool IsGroup, IReadOnlyCollection<Guid> ParticipantIds);
public record MessagePreviewDto(Guid Id, string EncryptedContent, DateTimeOffset CreatedAt, Guid SenderId);
public record ConversationDto(Guid Id, string Title, string Type, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyCollection<UserDto> Participants, MessagePreviewDto? LastMessage);
public record SendMessageRequest(Guid ConversationId, string EncryptedContent, string? MediaUrl = null, string? MediaContentType = null);
public record MessageDeliveryDto(Guid UserId, string Status, DateTimeOffset? DeliveredAt, DateTimeOffset? ReadAt);
public record MessageDto(Guid Id, Guid ConversationId, Guid SenderId, string EncryptedContent, string? MediaUrl, string? MediaContentType, bool IsDeletedForEveryone, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt, IReadOnlyCollection<MessageDeliveryDto> Deliveries);
public record MediaUploadResponse(Guid Id, string Url, string FileName, string ContentType, long SizeBytes);
