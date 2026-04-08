namespace AxonWeave.Application.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public string RootPath { get; set; } = "uploads";
    public string? PublicBaseUrl { get; set; }
}
