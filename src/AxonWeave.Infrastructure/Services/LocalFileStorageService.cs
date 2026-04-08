using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AxonWeave.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public LocalFileStorageService(
        IOptions<StorageOptions> options,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _options = options.Value;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
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
        var url = $"{ResolvePublicBaseUrl().TrimEnd('/')}/{storedFileName}";
        return (url, storedFileName, size);
    }

    private string ResolvePublicBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return _options.PublicBaseUrl;
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null)
        {
            return $"{request.Scheme}://{request.Host}/uploads";
        }

        var renderHostname = _configuration["RENDER_EXTERNAL_HOSTNAME"];
        if (!string.IsNullOrWhiteSpace(renderHostname))
        {
            return $"https://{renderHostname}/uploads";
        }

        return "http://localhost:8080/uploads";
    }
}
