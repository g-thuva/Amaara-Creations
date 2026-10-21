using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace be.Api.Middleware;

// Cookie-authenticated refresh/logout and login must not be triggerable by another
// website. CORS alone does not reject state-changing simple cross-origin requests.
public sealed class AuthOriginMiddleware(RequestDelegate next, IOptions<CorsOptions> cors)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            var origin = context.Request.Headers.Origin.ToString();
            var fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
            var policy = cors.Value.GetPolicy("AllowReactApp");
            var ownOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
            if ((origin.Length > 0 && origin != ownOrigin && policy?.IsOriginAllowed(origin) != true) ||
                (origin.Length == 0 && fetchSite == "cross-site"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }
        await next(context);
    }
}
