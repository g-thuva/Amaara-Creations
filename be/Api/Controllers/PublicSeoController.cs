using be.Application.Features.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/seo"), AllowAnonymous]
public sealed class PublicSeoController(PublicSeoService seo) : ControllerBase
{
    [HttpGet("robots.txt")]
    public IActionResult Robots() => Content(seo.Robots(), "text/plain");
    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken ct) => Content(await seo.Sitemap(ct), "application/xml; charset=utf-8");
}
