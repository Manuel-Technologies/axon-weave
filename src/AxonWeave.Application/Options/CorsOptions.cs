namespace AxonWeave.Application.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";
    public bool AllowAnyOrigin { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new();
}
