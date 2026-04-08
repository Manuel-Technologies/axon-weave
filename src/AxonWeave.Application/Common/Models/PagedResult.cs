namespace AxonWeave.Application.Common.Models;

public class PagedResult<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }
    public int Count => Items.Count;
}
