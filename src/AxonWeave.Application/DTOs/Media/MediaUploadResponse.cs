namespace AxonWeave.Application.DTOs.Media;

public class MediaUploadResponse
{
    public required Guid Id { get; init; }
    public required string Url { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
}
