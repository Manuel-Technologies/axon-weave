using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.Options;
using Microsoft.Extensions.Options;

namespace AxonWeave.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IOptions<StorageOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<(string url, string storedFileName, long size)> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var rootPath = Path.Combine(_environment.ContentRootPath, _options.RootPath);
        Directory.CreateDirectory(rootPath);

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(rootPath, storedFileName);

        await using var output = File.Create(filePath);
        await stream.CopyToAsync(output, cancellationToken);

        var size = output.Length;
        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/{storedFileName}";
        return (url, storedFileName, size);
    }
}
