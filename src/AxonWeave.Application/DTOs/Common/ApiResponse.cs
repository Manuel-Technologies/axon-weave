namespace AxonWeave.Application.DTOs.Common;

public class ApiResponse<T>
{
    public required T Data { get; init; }
}
