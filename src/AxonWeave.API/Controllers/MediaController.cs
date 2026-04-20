using AxonWeave.API.Abstractions;
using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.DTOs.Common;
using AxonWeave.Application.DTOs.Media;
using AxonWeave.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxonWeave.API.Controllers;

/// <summary>
/// Uploads media files for later use in chat messages.
/// </summary>
[Authorize]
[Route("api/media")]
public class MediaController : AuthenticatedControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public MediaController(IFileStorageService fileStorageService, IUnitOfWork unitOfWork)
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    [ProducesResponseType(typeof(ApiResponse<MediaUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    /// <summary>
    /// Uploads a file using multipart form data and returns the stored media metadata and public URL.
    /// </summary>
    public async Task<ActionResult<ApiResponse<MediaUploadResponse>>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "Uploaded file is empty." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _fileStorageService.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);

        var media = new MediaAsset
        {
            UploadedByUserId = GetUserId(),
            OriginalFileName = file.FileName,
            StoredFileName = result.storedFileName,
            ContentType = file.ContentType,
            SizeBytes = result.size,
            Url = result.url
        };

        await _unitOfWork.MediaAssets.AddAsync(media, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<MediaUploadResponse>
        {
            Data = new MediaUploadResponse
            {
                Id = media.Id,
                Url = media.Url,
                FileName = media.OriginalFileName,
                ContentType = media.ContentType,
                SizeBytes = media.SizeBytes
            }
        });
    }
}
