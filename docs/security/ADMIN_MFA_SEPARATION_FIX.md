# Admin MFA and customer authentication separation

The entry-page policy in this historical report was superseded on 2026-11-06 by the explicit requirement for a shared login and signup. See [Shared login and staff MFA](SHARED_LOGIN_AND_STAFF_MFA.md). Management and customer sessions remain separate; staff now complete their required MFA on the shared page.

Verification date: 2026-11-05. Changes are local; nothing was pushed or deployed.

## A. Root Cause Analysis

**Confirmed separation defect:** `IdentityService.VerifyPasswordAsync` checked privileged eligibility only when the management flag was true. Customer entry therefore accepted known privileged roles. Shared `IssueSignInAsync` subsequently created both storefront and management refresh families for staff. `SignInResponse` returned the storefront token to the shared form. Customer account controllers generally required only authentication, so privileged identities could act on customer functionality without an eligible customer session.

**Admin workflow coupling:** management `StaffApp` redirected unauthenticated users to the storefront. `authApi` hardcoded customer sign-in and MFA URLs even though management restoration used `/auth/staff/refresh`. The returned storefront JWT had `session_kind=storefront`; staff policies require `management` plus verified MFA. The shared flow depended on a subsequent cookie exchange after cross-origin navigation instead of completing the dedicated privileged flow in its own application.

The original isolated baseline passed 31 backend authentication checks and two real browser journeys, including MFA/dashboard/reload. Consequently, the reported return-to-login incident did **not** reproduce in that baseline. There is no evidence from that run that cookies were missing, roles were malformed, or a guard redirected prematurely. The confirmed shared-flow defects have been removed and the corrected journey is verified; the precise cause of the earlier incident remains unconfirmed without its failing environment/trace.

Read-only inspection of the configured Development database confirmed an email-verified account with both `SuperAdmin` and `Admin`, MFA enabled, administration enabled, bootstrap disabled, and no pending migrations. Existing role claim configuration was retained. No real passwords, authenticator codes or recovery secrets were submitted or logged.

## B. Code Changes

| Area | Important files | Result |
| --- | --- | --- |
| Eligibility and issuance | `be/Domain/Identity/AppRoles.cs`, `be/Application/Features/Identity/IdentityService.cs` | Authoritative scoped eligibility; one permitted session type; no privileged customer fallback |
| MFA transaction | `be/Application/Abstractions/Identity/IPendingSignInService.cs`, `be/Infrastructure/Identity/SecondFactorService.SignIn.cs` | Protected pending state binds its authentication intention; wrong-scope exchange fails before consumption |
| API and cookies | `be/Api/Controllers/StaffAuthController.cs`, `AuthController.cs`, `be/Api/Identity/RefreshCookie.cs`, `SignInResponse.cs` | Dedicated staff challenge/verification/enrollment/completion/cancellation; distinct pending cookies; scope-specific logout and security cookie clearing |
| API authorization | `be/Api/Extensions/AuthenticationExtensions.cs`, `be/Application/Features/Identity/AppPolicies.cs`, customer controller attributes | Current account eligibility on bearer validation; explicit CustomerAccess on customer operations |
| Client authentication | `client/src/features/auth/AuthProvider.jsx`, `authApi.js`, `sessionIdentity.js`, `hooks/useLogin.js`, login components | Scoped endpoints/restoration; validate complete roles; retain MFA form and recoverable errors during submission |
| Routing | `client/src/features/admin/app/StaffApp.jsx`, `client/src/app/guards/AuthGuard.jsx`, `AdminGuard.jsx`, `loginDestination.js` | Management verifies permissions before choosing existing routes; customer guard rejects privileged identities; obsolete shared handoff removed |
| Regressions | New `AuthenticationSeparationTests.cs`, `MfaChallengeScopeTests.cs`, `authentication-separation.spec.ts`, role unit tests; existing auth/SQL/browser tests | Negative scope, mixed-role, replay, restoration, recovery and logout coverage |

Existing uncommitted persistent-session work was preserved. This fix adds no database migration. Payment implementation, webhook handling and business calculations were not rewritten; the customer payment-status action only received its required authorization policy. Shared private artwork/proof downloads retain their existing staff authorization.

## C. Corrected Authentication Flow

Customers submit credentials to `/api/v1/auth/sign-in` or its `/login` alias. The server validates credentials and current roles, rejects privileged accounts, and creates only a customer session for an eligible identity. Ordinary customer login does not require MFA; deliberately enrolled optional customer MFA remains supported. The storefront navigates to a safe customer destination.

Staff submit credentials through the management application's `/login` route to `/api/v1/auth/staff/sign-in` (also `/staff/login`). Password and current privileged eligibility are verified before a restricted HTTP 202 challenge. `/staff/sign-in/verify` verifies an existing authenticator. Enrollment uses `/staff/sign-in/enrollment/confirm` followed by acknowledgment at `/staff/sign-in/complete`. Only completion creates a management family, management access token, protected refresh cookie and session-bound MFA assurance.

The management provider stores its access token in memory and its authenticated identity in React state. `StaffApp` waits for `/auth/staff/session` to return current permissions before using `adminStart` and the existing route metadata. Admin reaches `/admin/dashboard`; specialists reach an authorized existing interface. A valid session at management `/login` restores the workspace. Password reset routes remain available.

## D. Role Separation

The actual privileged roles are Admin, SuperAdmin, CatalogManager, ProductionStaff, CustomerSupport, Marketing and Analyst. Membership in any of them excludes customer authentication, even when Customer is also present. Unknown or unresolved roles fail closed. Roles are not changed to make routing work.

CustomerAccess requires an authenticated storefront session, Customer membership and no privileged membership. It protects profile, addresses, cart, wishlist, checkout, personal orders, customer payment status, personal reviews, customer requests and customer design operations. Staff policies retain their existing permission assignments and MFA requirements.

Customer and management pending cookies use distinct names and paths. The challenge itself also records scope, so copying its opaque value under the other cookie name cannot convert it. Refresh validates current eligibility and cannot upgrade customer assurance to management. Privileged storefront sessions left by the older implementation fail closed.

Separate eligible customer and staff identities may be explicitly signed in within one browser. Neither scope automatically signs in or overwrites the other. Logout clears its own scope; logout-all revokes the authenticated user's server sessions and preserves an independently authenticated identity's cookie. Existing idle/absolute deadlines and original MFA time are preserved during rotation.

## E. MFA Verification

Observed real HTTP sequence: staff credential POST **202**, code verification **200**, staff session/permission GET **200**, protected dashboard API **200**, dashboard reload and staff refresh **200**. The verified management identity could call admin APIs while customer APIs returned **403**. No customer refresh cookie was created.

Wrong codes retain the verification form and produce no authenticated session. Network failure permits explicit retry without automatic MFA resubmission. Expired/changed challenges fail, consumed challenges cannot create another session, and revoked or expired sessions require privileged reauthentication. SQL tests verify one challenge winner across two API instances and restoration across API restarts. TOTP enrollment, native verification, encrypted key storage and replay protections were retained.

## F. Test Results

| Verification | Result |
| --- | --- |
| Full backend suite | **458 passed**, 18 SQL-dependent tests skipped in this run |
| Separate real SQL MFA/refresh suite | **3 passed**: restart persistence, cross-instance challenge consumption, concurrent refresh rotation |
| Frontend unit suite | **265 passed** across 37 files |
| Optimized-build browser authentication/session suite | **38 passed**, including journeys A-F, both login boundaries, specialists, enrollment/recovery, reload, browser restart, idle/absolute expiry, revocation, delayed restoration, separate identities and logout |
| Additional optimized-build authorization boundaries | **2 passed**: privileged storefront rejection and owner/staff private artwork access |
| Development access-expiry browser checks | Customer and admin expired JWTs renewed once through the real backend; concurrent requests safely retried without navigation |
| Frontend builds | Storefront and admin builds passed |
| TypeScript and focused authentication lint | Passed |
| Storefront artifact isolation and diff whitespace | Passed |

Browser requests used the actual application controllers, ASP.NET Core Identity, native TOTP verification, JWTs and refresh service through the separate acceptance executable with disposable identities. Browser persistence was in the acceptance database; the separate SQL suite exercised real SQL Server transactions. No mocked authentication responses were substituted. Synthetic fixture emails/payments are unrelated test infrastructure and are not a claim of real email or payment-provider verification.

Local evidence is in `.runtime/mfa-fix-backend-final.log`, `mfa-fix-sql.log`, `mfa-fix-unit-final.log`, `mfa-fix-browser-complete.log`, `mfa-fix-browser-production.log`, `mfa-fix-browser-recovery-final.log`, `mfa-fix-browser-boundaries.log`, and `mfa-fix-dashboard.png`. Temporary servers and compiled previews were isolated from the configured business application.

The final browser selection excludes an unrelated existing responsive-header assertion and the two development-source JWT injection cases (those passed separately in Development). Full-project lint still reports an unused `idx` in `InstagramShowcase.jsx` and an existing `CustomDropdown.jsx` effect-dependency warning. A broader responsive run found the unmodified storefront desktop header overflowing at 360px. These were left unchanged to preserve the requested scope; the complete repository is not being represented as having every quality check green.

Framework references: [ASP.NET Core JWT validation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-8.0) and [credentialed CORS configuration](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-8.0). Existing explicit claim mapping, trusted-origin checks and secure cookie architecture remain enforced.

## G. Remaining Issues

The corrected admin MFA and customer separation journeys pass locally, including optimized frontend artifacts. The earlier login-loop incident's specific failing boundary remains unconfirmed because the original isolated baseline also succeeded. This report does not claim that its exact historical cause was reproduced.

The owner's physical authenticator/account and deployed hosting environment were not used for automated verification. Nothing was pushed or deployed. Production origin/TLS/key-ring configuration is therefore outside this local verification claim. The unrelated lint and storefront-header issues above remain.

No real accounts, passwords, memberships, MFA enrollment or business data were changed. The existing persistent-session migration files were already present before this task and were not introduced by this separation fix.
