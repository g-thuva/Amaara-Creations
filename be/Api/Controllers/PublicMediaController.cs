using be.Application.Abstractions.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;

[ApiController, Route("api/v1/media/public"), AllowAnonymous]
public sealed class PublicMediaController(IPublicMediaDelivery delivery) : ControllerBase
{
    [HttpGet("{**key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var result = await delivery.ReadAsync(key, ct);
        if (result == null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        if (result.RedirectUrl != null)
        {
            Response.Headers.CacheControl = "public, max-age=300";
            return Redirect(result.RedirectUrl);
        }
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(result.Content!, "image/png", enableRangeProcessing: false);
    }
}
