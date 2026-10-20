using be.Api.Configuration;
using be.Domain.Common;
using Microsoft.Extensions.Options;

namespace be.Api.Middleware;

public sealed class ReleaseControlsMiddleware(RequestDelegate next, IOptionsMonitor<ReleaseControls> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var path = (request.Path.Value ?? string.Empty).TrimEnd('/');
        var post = HttpMethods.IsPost(request.Method);
        // Existing financial callbacks must remain verifiable during a new-sales pause.
        var webhook = path.Equals("/api/v1/payments/zahls/webhook", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/payments/datatrans/webhook", StringComparison.OrdinalIgnoreCase);
        var controls = options.CurrentValue;
        var newPayment = post && (path.Equals("/api/v1/checkout/session", StringComparison.OrdinalIgnoreCase)
            || (request.Path.StartsWithSegments("/api/v1/checkout/orders", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/retry", StringComparison.OrdinalIgnoreCase)));
        var write = !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method);
        var upload = write && request.Path.StartsWithSegments("/api/v1") && request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;
        var adminWrite = write && request.Path.StartsWithSegments("/api/v1/admin", StringComparison.OrdinalIgnoreCase);
        if (!webhook && ((!controls.AcceptNewPayments && newPayment) || (!controls.AcceptUploads && upload) || (!controls.AllowAdminChanges && adminWrite)))
        {
            context.Response.Headers.RetryAfter = "300";
            throw new CommerceException("RELEASE_OPERATIONS_PAUSED", "This operation is temporarily unavailable. Please try again later.", 503);
        }
        await next(context);
    }
}
