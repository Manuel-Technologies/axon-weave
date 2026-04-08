using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AxonWeave.API.Abstractions;

[ApiController]
public abstract class AuthenticatedControllerBase : ControllerBase
{
    protected Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("Missing authenticated user id.");
        }

        return userId;
    }
}
