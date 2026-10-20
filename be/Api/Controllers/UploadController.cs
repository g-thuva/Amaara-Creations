using System.Security.Claims;
using be.Api.Files;
using be.Application.Abstractions.Storage;
using be.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/upload")]
public sealed class UploadController(IAvatarStorage avatars) : ControllerBase
{
    [HttpPost("avatar"), Authorize, EnableRateLimiting("custom-upload"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult> UploadAvatar(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new CommerceException("AUTHENTICATION_REQUIRED", "User not found", 401);
        var saved = await avatars.SaveAsync(file.ToMediaUpload(), userId, HttpContext.RequestAborted);
        return Ok(new { message = "Avatar uploaded successfully", fileUrl = saved.FileUrl, fileName = saved.FileName });
    }
}
