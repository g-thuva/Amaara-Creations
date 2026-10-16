using System.Security.Claims;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;

namespace be.Tests;
internal sealed class TestRequestActor(string? userId = null, params string[] roles) : IRequestActor
{
    public ClaimsPrincipal User { get; } = new(new ClaimsIdentity((userId == null ? Array.Empty<Claim>() : [new Claim(ClaimTypes.NameIdentifier, userId)]).Concat(roles.Select(role => new Claim(ClaimTypes.Role, role))), "test"));
    public CancellationToken RequestAborted => default;
    public AuditActor Audit => new(userId, "test-correlation", "127.0.0.1");
    public SessionMetadata Session => new("test-agent", "127.0.0.1");
}
