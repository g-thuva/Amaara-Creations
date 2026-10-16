using be.Application.Abstractions.Persistence;
using be.Domain.Common;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace be.Application.Features.Cms;
public sealed class PublicSeoService(IApplicationDbContext db, IConfiguration config)
{
    private string? BaseUrl()
    {
        var value = config["PublicSite:BaseUrl"];
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) ? uri.AbsoluteUri.TrimEnd('/') : null;
    }

    public string Robots()
    {
        var origin = BaseUrl();
        if (origin == null)
            return "User-agent: *\nDisallow: /\n";
        var prefix = new Uri(origin).AbsolutePath.TrimEnd('/');
        var privatePaths = new[]
        {
            "/admin",
            "/account",
            "/profile",
            "/orders",
            "/addresses",
            "/cart",
            "/checkout",
            "/wishlist",
            "/login",
            "/register",
            "/forgot-password",
            "/reset-password",
            "/verify-email",
            "/custom/"
        };
        return "User-agent: *\n" + string.Join("\n", privatePaths.Select(p => "Disallow: " + prefix + p)) + "\nDisallow: /api/\nSitemap: " + origin + "/sitemap.xml\n";
    }

    public async Task<string> Sitemap(CancellationToken ct)
    {
        var origin = BaseUrl();
        if (origin == null)
            throw new CommerceException("PUBLIC_SITE_UNCONFIGURED", "The approved public site URL is not configured.", 503);
        var products = await db.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Id).Select(p => new { p.Id, p.UpdatedAt }).ToListAsync(ct);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var nodes = new[]
        {
            "/",
            "/products",
            "/custom",
            "/about",
            "/contact"
        }.Select(path => new XElement(ns + "url", new XElement(ns + "loc", origin + path)))
            .Concat(products.Select(p => new XElement(ns + "url", new XElement(ns + "loc", origin + "/products/" + p.Id), new XElement(ns + "lastmod", p.UpdatedAt.ToUniversalTime().ToString("yyyy-MM-dd")))));
        return new XDocument(new XElement(ns + "urlset", nodes)).ToString();
    }
}
