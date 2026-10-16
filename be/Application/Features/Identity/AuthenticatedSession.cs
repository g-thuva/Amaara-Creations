namespace be.Application.Features.Identity;
// Only Response is an API contract. The refresh secret is written to an HttpOnly cookie.
public sealed record AuthenticatedSession(AuthResponse Response, string RefreshToken, DateTime RefreshExpiresAt);
