# Shared authentication and MFA

The public `/login` screen owns credential entry, verification, enrollment and recovery. ASP.NET Core Identity is the source of identity and current roles. Existing privileged roles are `Admin`, `SuperAdmin`, `CatalogManager`, `ProductionStaff`, `CustomerSupport`, `Marketing` and `Analyst`; `SuperAdmin` is the existing owner equivalent. No role is inferred from an email address, browser storage or a selected account mode.

## State and session boundaries

| Verified account | Next action | Session issued |
| --- | --- | --- |
| Customer without optional MFA | Complete password authentication | Shopping only |
| Existing privileged enrollment | Current authenticator code | None until verification |
| Privileged account without enrollment | Stable QR/manual setup, verify code, save recovery codes | None until acknowledgment |
| Privileged account using recovery | Replace authenticator, verify new code, save new recovery codes | None until acknowledgment |
| Unknown roles, unconfirmed/locked account, disabled management | Safe rejection | None |

Existing customers who deliberately enabled optional MFA retain it. This preserves their enrolled protection and recovery information; ordinary customers are never required to enroll. Password reset does not remove MFA or change memberships.

`POST /api/v1/auth/sign-in` verifies the password and reads current roles. Pending responses are HTTP 202 with the next stage, expiry, and only that stage's data. An opaque Data Protection ticket is an HttpOnly cookie scoped to `/api/v1/auth`; the protected state lives in existing `UserTokens` under `Amaara.SignIn.v1`. Neither is a bearer token, refresh session or management grant. No normal JWT is issued during pending staff authentication, including recovery-code acknowledgment.

`GET /auth/sign-in` restores the current stage. POST operations `/verify`, `/enrollment/confirm`, `/complete` and `/cancel` advance it. Challenges bind a random nonce hash to user ID, security stamp, exact role set, expiry and stage. There is one current pending transaction per user; a new password submission replaces the previous ticket. SQL application locks serialize per-user verification across API instances. Consumption is one-time. Password/security/role changes, lockout, cancellation and expiry invalidate progress. Failed attempts commit before returning an error.

Pending login and QR enrollment last at most ten minutes. Confirmed enrollment has five minutes for recovery-code acknowledgment. Refreshing reuses the same unexpired QR and recovery-code panel. These are private, no-store responses. Five failed challenge verifications end that transaction; Identity's configured lockout also accumulates failures. Existing password/MFA rate limiters and trusted-origin middleware remain active.

After required verification, the API creates independent shopping and management refresh-session families. The public response exposes only a shopping access token; management refresh and assurance use separate protected HttpOnly staff-path cookies. The approved management origin restores its own bearer token, verifies `/auth/staff/session`, and loads operational modules only after current permissions are authorized. Management `/login` restores a valid session or returns to shared credential entry. Validated management deep links survive this round trip. No credentials, MFA proof or tokens are carried in navigation URLs or browser storage.

Legacy `/auth/login` and `/auth/staff/login` delegate to this same service. Complete existing clients may submit both factors together, subject to identical native verification, replay and role checks. Direct management completion preserves an existing shopping cookie. Missing factors receive the limited pending transaction. These aliases do not provide another public form or a bypass.

## TOTP and storage

`SecondFactorService` uses the installed Identity `AuthenticatorTokenProvider` through `VerifyTwoFactorTokenAsync`; production code no longer computes its own TOTP. Six-digit SHA-1 RFC 6238 codes use 30-second steps. The .NET 8 provider accepts the current step and two steps either side (about ±60 seconds); replay hashes remain three minutes. Keep all API instance clocks synchronized with monitored NTP; do not widen acceptance to hide a clock fault. See [Identity provider API](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.authenticatortokenprovider-1).

Each pending setup has a cryptographically random Identity-generated key and standard `otpauth://totp` URI, issuer `Amaara Creations`, label `Amaara Creations:<verified email>`. `qrcode.react` renders the QR locally. No external QR server receives the secret. The shared QR/manual-key component supports copy and a user-initiated same-device authenticator link. Production requires HTTPS and private no-store surfaces.

`ProtectedIdentityUserStore` writes authenticator keys as `protected:v1:` Data Protection ciphertext in the existing Identity token row. Unprefixed legacy keys remain readable and are protected only after successful native verification, without changing the logical enrollment. Invalid ciphertext or invalid legacy secrets fail closed and require verified recovery. Pending setup/state, recovery hashes and replay records are also protected. Recovery codes contain 96 random bits each, are stored as SHA-256 hashes inside protected token payloads, and are consumed atomically. Plain codes persist only in the encrypted five-minute acknowledgment transaction, then are inaccessible through account endpoints.

Persist the same Data Protection application name (`AmaaraCreations`), key ring and decryption credentials across restarts/instances. Encrypt the production key ring at rest with the host's certificate/KMS/volume mechanism, restrict filesystem/service-account access and back up keys through the approved private recovery procedure. Never commit keys or copy development enrollment into production. Losing the key ring can invalidate protected enrollments, tickets, recovery payloads and cookies.

## Authorization and sensitive changes

JWT validation checks signature, algorithm, issuer, audience, lifetime, confirmed/unlocked account, security stamp and active refresh family, then reloads current roles. Management additionally requires enabled MFA and `session_kind=management`. Staff policies require the effective permission and trusted `mfa` evidence no older than fifteen minutes; sensitive policies (staff changes/MFA recovery, refunds, reports/exports and settings) require five minutes. Refresh preserves the original verification time and cannot renew assurance. Staff refresh with missing/expired assurance or incomplete enrollment revokes the rotated token and returns 401, including legacy limited sessions. Reauthentication returns to shared sign-in; it does not prompt on every code rotation.

Authenticator confirmation atomically rotates the stamp, enables the verified key, invalidates prior recovery codes and revokes all refresh sessions. Notifications contain no secrets; delivery failure records `AuthenticatorNotificationDeferred` for operator follow-up. Recovery approval and replacement are audited. See [recovery procedures](mfa-recovery.md).

## Configuration, compatibility and rollback

No Identity schema or historical EF migration changes are needed. Existing passwords, users, memberships and valid keys remain intact. The two earlier business migrations are a separate operational concern; they were not applied to the owner's database.

Build storefront and management artifacts separately. Set approved HTTPS `VITE_MANAGEMENT_LOGIN_URL` in the storefront and `VITE_PUBLIC_SITE_URL` in management. Configure exact general CORS origins and management-only `Administration:AllowedOrigins`. Staff refresh/session/logout must remain unavailable from the storefront origin. Deploy on a reviewed same-site cookie arrangement or BFF; unrelated sites cannot assume third-party cookies work. See [deployment controls](ADMIN_ISOLATION_DEPLOYMENT.md).

For the first encryption-format cutover, pause authentication traffic and drain old API instances; deploy the decrypt-aware version with the same durable key ring on every instance before resuming traffic. Mixed old/new instances are unsupported once a protected key is written. An older binary that treats ciphertext as a raw TOTP secret is **not a compatible rollback**. Retain this reader in any rollback package or use a forward fix with administration disabled, preserved keys and audited session revocation. Do not mass-decrypt secrets, reset enrollments or restore a stale business database to roll back application code.
