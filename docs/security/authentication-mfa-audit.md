# Authentication and MFA audit

## Starting point and confirmed issues

The active local branch is `fix/admin-auth-dashboard` (based on `develop` at `0e3d890`). It contains verified authentication fixes and subsequent owner-approved local changes; resetting to develop would discard them. The stack is React/Vite, ASP.NET Core 8 Identity/JWT and EF Core SQL Server. Canonical roles are Customer, Admin, SuperAdmin and the existing specialist roles; SuperAdmin is the owner equivalent. No new Staff/Owner role is needed.

- `useLogin` originally ignored returned roles and always chose a customer return such as `/profile`. The current role-aware redirect fixed that, but redirect-only login originally repeated staff credentials.
- The latest `/auth/sign-in` verifies a password and optional code together and creates shopping/staff sessions immediately. Unenrolled staff receive limited bearer sessions and perform setup in the management application. This conflicts with the new requirement that pending staff receive no ordinary JWT and complete setup on the shared login screen.
- `LoginForm` exposes authenticator/recovery controls to ordinary customers before any verified role is known. Staff must guess whether a code is needed.
- `AuthenticatorSettings` offers only a manual key, repeats password entry for starting and confirming setup, then logs out and requests another password/code. It has no QR, recovery download/print/copy, guided progress, or saved-code acknowledgment.
- `SecondFactorService` correctly serializes SQL verification, protects enrollment/replay/recovery token payloads and rejects consumed codes, but implements its own TOTP calculation. Identity's supported authenticator provider should perform verification. The default Identity authenticator-key row is not additionally Data Protection encrypted.
- Customer optional MFA already exists. Preserve valid opt-in enrollment and recovery; mandatory MFA applies only to current privileged roles. Opted-in customers retain their chosen verification rather than silently losing protection.
- Staff JWT policies correctly require current roles, a management session and recent verified MFA. JWT signature/issuer/audience/stamp/session-family checks, refresh rotation, lockout, rate limiting and origin restrictions must stay enforced.
- Actual local Identity mappings are `Users`, `Roles`, `UserRoles`, `UserTokens`. Existing memberships/enrollment must be preserved. The owner declined business-database copying and migration; no schema change or actual-account enrollment is authorized or necessary.

## Selected consolidation

Refactor the current sign-in operation into one password-first transaction. Use an opaque, protected HttpOnly challenge cookie and an encrypted, expiring record in existing Identity UserTokens; this is not an authentication/refresh session. Recheck stamp, roles, lockout and management availability on every transition. Serialize consumption across SQL instances. Expose only the next required action after the password succeeds.

Customers without opt-in MFA complete immediately. Returning staff enter a TOTP challenge; new staff see one stable QR/manual enrollment, verify it, save recovery codes and continue without re-entering a password. Verified recovery permits protected authenticator replacement, not management access. Issue independent final shopping/management sessions only after the required proof and acknowledgment. Keep operational code in the separate artifact; its login entry restores the final staff cookie or returns to the shared login. Existing direct API aliases use the same service and cannot issue privileged access before MFA.

Reuse the existing second-factor service, SQL locks, hashed recovery codes, replay tracking, security-stamp rotation and authorization policies. Replace cryptographic verification with Identity's registered authenticator provider and encrypt new authenticator keys through the Identity store while reading existing keys compatibly. Generate QR images locally; no external service receives enrollment secrets. Persist pending enrollment so refresh cannot regenerate the QR or lose recovery acknowledgment.

## Implementation and verification plan

1. Preserve the tested local baseline in a reviewable commit, then implement transaction/state and second-factor refactoring.
2. Add the shared progressive login, local QR/manual setup and recovery controls using existing branding/components.
3. Test pending-token denials, expiry, replay, bad codes, recovery replacement, demotion, password reset, existing enrollment compatibility, session isolation and final redirects with real Identity and disposable stores.
4. Verify browser journeys, keyboard/paste/loading/error states and the requested 360/390/768/1024/1440 widths, QR decoding and production artifacts.
5. Record observed results and deployment requirements. A physical Google Authenticator scan and external production deployment require owner/device/hosting verification; automated QR decoding plus independent TOTP fixtures does not claim those occurred.

Framework references: [Identity MFA](https://learn.microsoft.com/aspnet/core/security/authentication/mfa), [AuthenticatorTokenProvider](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.authenticatortokenprovider-1), [Identity authenticator-key store](https://source.dot.net/Microsoft.Extensions.Identity.Stores/UserStoreBase.cs.html). Official app destinations were verified for [Google Play](https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2) and [Apple App Store](https://apps.apple.com/app/google-authenticator/id388497605).
