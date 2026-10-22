namespace be.Application.Features.Identity;
// Only Response is an API contract. The refresh secret is written to an HttpOnly cookie.
public sealed record StaffAssurance(string UserId, string SecurityStamp, string SessionFamily, long VerifiedAt);
public sealed record AuthenticatedSession(AuthResponse Response, string RefreshToken, DateTime RefreshExpiresAt, StaffAssurance? StaffAssurance = null);
public sealed record RoutedSignIn(AuthenticatedSession? Storefront, AuthenticatedSession? Management);
public sealed record SignInResult(be.Application.Abstractions.Identity.SignInStep? Pending = null, RoutedSignIn? Sessions = null);
