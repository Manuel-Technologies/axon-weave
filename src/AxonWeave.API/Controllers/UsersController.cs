using AxonWeave.API.Abstractions;
using AxonWeave.API.Services;
using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.DTOs.Common;
using AxonWeave.Application.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AxonWeave.API.Controllers;

/// <summary>
/// Provides authenticated user discovery endpoints.
/// </summary>
[Authorize]
[Route("api/users")]
public class UsersController : AuthenticatedControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<UserDto>>), StatusCodes.Status200OK)]
    /// <summary>
    /// Searches users by phone number fragment.
    /// </summary>
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> Search([FromQuery] string? phone, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Users.Query().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalized = phone.Trim();
            query = query.Where(x => x.PhoneNumber.Contains(normalized));
        }

        var users = await query.OrderBy(x => x.Name).Take(50).ToListAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyCollection<UserDto>>
        {
            Data = users.Select(x => x.ToDto()).ToList()
        });
    }
}
