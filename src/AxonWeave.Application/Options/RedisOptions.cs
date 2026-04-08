namespace AxonWeave.Application.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = "localhost:6379";
    public int PresenceTtlSeconds { get; set; } = 120;
}
