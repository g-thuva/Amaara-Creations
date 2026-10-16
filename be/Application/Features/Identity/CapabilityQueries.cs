using be.Application.Abstractions.Identity;

namespace be.Application.Features.Identity;
public sealed class CapabilityQueries(IRequestActor _actor)
{
    public object? Capabilities() => AppPolicies.Roles.Where(p => p.Value.Any(_actor.User.IsInRole)).Select(p => p.Key);
}
