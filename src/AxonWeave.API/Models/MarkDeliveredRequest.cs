namespace AxonWeave.API.Models;

public class MarkDeliveredRequest
{
    public List<Guid> MessageIds { get; set; } = new();
}
