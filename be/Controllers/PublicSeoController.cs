using System.Xml.Linq;
using be.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController, Route("api/v1/seo"), AllowAnonymous]
public sealed class PublicSeoController(ApplicationDbContext db, IConfiguration config) : ControllerBase
{
    private string? BaseUrl()
    {
        var value = config["PublicSite:BaseUrl"];
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
            ? uri.AbsoluteUri.TrimEnd('/') : null;
    }
    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var origin = BaseUrl();
        if (origin == null) return Content("User-agent: *\nDisallow: /\n", "text/plain");
        var prefix = new Uri(origin).AbsolutePath.TrimEnd('/');
        var privatePaths = new[] { "/admin", "/account", "/profile", "/orders", "/addresses", "/cart", "/checkout", "/wishlist", "/login", "/register", "/forgot-password", "/reset-password", "/verify-email", "/custom/" };
        return Content("User-agent: *\n" + string.Join("\n", privatePaths.Select(p => "Disallow: " + prefix + p)) + "\nDisallow: /api/\nSitemap: " + origin + "/sitemap.xml\n", "text/plain");
    }
    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        var origin = BaseUrl();
        if (origin == null) return Problem(statusCode: 503, title: "The approved public site URL is not configured.");
        var products = await db.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Id).Select(p => new { p.Id, p.UpdatedAt }).ToListAsync(ct);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var nodes = new[] { "/", "/products", "/custom", "/about", "/contact" }.Select(path => new XElement(ns + "url", new XElement(ns + "loc", origin + path)))
            .Concat(products.Select(p => new XElement(ns + "url", new XElement(ns + "loc", origin + "/products/" + p.Id), new XElement(ns + "lastmod", p.UpdatedAt.ToUniversalTime().ToString("yyyy-MM-dd")))));
        return Content(new XDocument(new XElement(ns + "urlset", nodes)).ToString(), "application/xml; charset=utf-8");
    }
}
