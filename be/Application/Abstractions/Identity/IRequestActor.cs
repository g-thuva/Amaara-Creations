using System.Security.Claims;
using be.Application.Abstractions.Audit;

namespace be.Application.Abstractions.Identity;
public interface IRequestActor
{
    ClaimsPrincipal User { get; }

    CancellationToken RequestAborted { get; }

    AuditActor Audit { get; }

    SessionMetadata Session { get; }
}
