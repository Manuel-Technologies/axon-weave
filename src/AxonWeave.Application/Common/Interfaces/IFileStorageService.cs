namespace AxonWeave.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<(string url, string storedFileName, long size)> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
