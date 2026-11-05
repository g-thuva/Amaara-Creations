namespace be.Application.Abstractions.Identity;
public sealed record SessionMetadata(string UserAgent, string? IpAddress, bool Management = false, DateTimeOffset? SecondFactorAt = null);
