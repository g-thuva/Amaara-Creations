using System.Security.Claims;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Observability;

namespace be.Api.Identity;
public sealed class HttpRequestActor(IHttpContextAccessor accessor) : IRequestActor
{
    public ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal();
    public CancellationToken RequestAborted => accessor.HttpContext?.RequestAborted ?? default;
    public AuditActor Audit => AuditFor(accessor.HttpContext);
    public SessionMetadata Session => new(accessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty, accessor.HttpContext?.Connection.RemoteIpAddress?.ToString());

    public static AuditActor AuditFor(HttpContext? context) => new(context?.User.FindFirstValue(ClaimTypes.NameIdentifier), context?.Items[RequestCorrelation.ItemName]?.ToString(), context?.Connection.RemoteIpAddress?.ToString());
}
