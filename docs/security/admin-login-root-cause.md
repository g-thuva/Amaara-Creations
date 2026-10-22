# Administrator sign-in diagnosis

Starting point: `develop` at `0e3d890`; focused branch `fix/admin-auth-dashboard`. Diagnosis recorded before authentication changes. This is a targeted correction to the existing separated application, not a return to shared-login routing.

## Confirmed observations

| Finding | Evidence | Consequence |
| --- | --- | --- |
| Public login deliberately returns to the customer profile | `client/src/features/auth/hooks/useLogin.js`: validated local return or `/profile`; public `main.jsx` never imports staff routes | Signing in on the storefront, including with staff credentials, is a shopping session. Adding a management redirect here would contradict the approved separated architecture |
| A separate management entry already exists | `client/src/admin-main.jsx`, Vite admin mode, capability check before lazy operational imports | Management sign-in belongs to the independent application; its entry is supplied through operator documentation |
| Configured local identity has both administrative roles | Parameterized read-only query against configured `localhost/AmaaraCreationsDB`, using configured bootstrap email internally; role rows `Admin` and `SuperAdmin`, email confirmed, MFA disabled | No missing membership, capitalization repair, password reset or role promotion is justified |
| Administration is closed locally | Effective `Administration:Enabled=false`; `StaffAuthorizationHandler` requires it plus recent verified MFA | Correct credentials alone cannot authorize operational APIs |
| Default development CORS recognizes only the shop | `StartupSettings.GetAllowedOrigins`: fallback ports 5173 only, while the independent admin script uses 5179 | A clean development configuration rejects management sign-in before credentials can be verified; development loopback management origins must be explicitly allowed |
| The actual database uses renamed Identity tables | EF `IdentityConfiguration.cs` and SQL metadata: `Users`, `UserRoles`, `Roles`, not `AspNet*` | Diagnostics must follow actual mappings; no destructive schema repair is needed |
| Management capability failures are swallowed or shown as a generic credential error | `admin-main.jsx` capability effect and submit catch | Disabled service, absent MFA, insufficient roles and expired sessions are indistinguishable |
| Restored management sessions remain on the sign-in form at `/login` | The form condition explicitly includes `pathname === '/login'` even after verified capabilities | Valid restoration does not consistently navigate to the permitted dashboard |
| Both applications share the same refresh cookie and authentication endpoints | `AuthProvider`, `authApi`, `RefreshCookie` | One application can replace the other's browser session; separated artifacts alone are not independent sessions |
| An initial refresh can finish after explicit sign-in | `AuthProvider` uses only effect lifetime cancellation, with no authentication-operation generation | A stale restore failure or response can overwrite a newer explicit session |

No password, hash, JWT, seed, recovery code, raw customer identity or full connection string was selected or logged. The configured database was only read; no existing records or migrations were changed. Bootstrap is disabled and will stay disabled.

## Existing controls verified in source

Identity resolves roles with `UserManager.GetRolesAsync`; `AppRoles` is canonical (`Customer`, `Admin`, `SuperAdmin`, and the existing specialist roles). JWTs use signed HS256, issuer/audience/type/lifetime checks, security-stamp/session-family binding and current database roles on validation. Authentication precedes authorization. Policies require recent MFA, with shorter assurance for sensitive actions. Customer routes use ownership checks. These controls must remain enforced.

## Selected fix

Keep customer login customer-facing and keep staff code out of its artifact. Give management dedicated server-selected login/refresh/logout paths and protected, separately named cookies; staff authorization must require a management session in addition to current roles and MFA. Provide authoritative management state, explicit enrollment/recovery/denial/expiry handling and permission-aware local return destinations. Prevent stale asynchronous authentication from overwriting newer operations. Retain current roles, pricing, data, design system and all operational policies. Supply read-only Identity inspection and an explicit, idempotent, non-production provisioning command; do not promote the existing account.

Local activation and genuine MFA enrollment are separate steps: the owner enters existing credentials privately and enrolls their own authenticator. No TOTP bypass, fabricated administrator credentials or automatic enrollment is allowed. Production administration remains disabled until its protected host and operational approvals are supplied.

## Reproduction and verification plan

1. On the storefront login, authenticate a disposable staff account: remain on the storefront, with no staff links or management URL redirect.
2. On the management login, test correct credentials with missing MFA, enrollment, valid MFA, valid restoration at `/login`, permitted deep links, insufficient permissions and expired assurance.
3. Delay a restore response across an explicit sign-in/logout; verify it cannot replace the newer session or expose protected content.
4. Keep customer and staff sessions in the same browser and API host; verify independent cookies, refresh and sign-out.
5. Exercise signed/forged/revoked tokens, demotion, raw API denials, missing roles, ownership and real-SQL behavior using disposable identities.

Local results do not establish deployed behavior. The public Pages workflow targets `main` and builds only the storefront. The following implementation and observed checks address the diagnosed issues without changing that deployment target.

## Implemented authentication and navigation

- `StaffAuthController`: dedicated management login, refresh, logout and authenticated session endpoints. Server code chooses the session kind; no role or management flag is accepted from the login DTO. Correct customer credentials receive 403 at the management entry and cannot create a management session. Wrong/missing credentials receive 401. A disabled management environment receives 503 after credential/role verification.
- `IdentityService`, `TokenService`, `AuthenticationExtensions`: roles remain authoritative Identity-store values; role/name claim mapping is explicit. Management tokens carry `session_kind=management`. Old/storefront tokens cannot satisfy staff policies even with roles and an MFA claim. Empty role metadata is no longer labeled Customer by default. Management sign-in outcomes are audited without credentials or email addresses.
- `RefreshCookie`, `AuthOriginMiddleware`, `StaffAuthorization`: management refresh cookies are independently named, HttpOnly, additionally Data Protection protected and scoped to `/api/v1/auth/staff`; production uses Secure/Lax. Management exchanges accept only exact `Administration:AllowedOrigins`, independently of general storefront CORS. The approved storefront origin is explicitly denied a management refresh/session inspection. Existing role, stamp, session-family and recent-MFA checks remain mandatory.
- `StaffApp`, `managementApi`, `admin-main`: initialization, authentication, enrollment, recovery, authorization, denial, expiry and service errors are distinct states. Backend session verification precedes lazy operational imports. Restored `/login` sessions reach the dashboard or permitted specialist workspace. Local authorized return paths survive; unsafe/customer paths cannot become management destinations. Focus/navigation/assurance expiry recheck access. Current server permissions drive navigation and guards.
- `AuthProvider`, `httpClient`, `authSession`, `authApi`: authentication generations suppress stale responses; explicit login/logout abort older refresh requests so their delayed Set-Cookie responses cannot erase newer cookies. Retry handling recognizes both authentication prefixes, avoids recursive refresh, and cannot let an older request clear a newer session. Logout clears the selected application's session; logout-all/security changes revoke all relevant families.
- `returnDestination`, `useLogin`, `AuthGuard`, permissions: validated app-local destinations reject external, protocol-relative, backslash, encoded slash, control-character and login-loop values. The storefront remains customer-facing, including for staff shopping. No management URL or menu is added to storefront navigation, footer, help or sitemap.

No schema migration was added by this fix. Historical migrations, orders, stored password hashes and existing role membership are unchanged. The existing session-lock implementation now safely reuses an owning transaction, permitting atomic operator role assignment, stamp rotation, session revocation and auditing; a disposable real-SQL test verifies it.

## Local operator procedure

The configured local account was read-only verified as `Admin` plus `SuperAdmin`, email confirmed, MFA disabled. No role assignment or password change was needed or performed. Its email/password are intentionally absent from this document.

```powershell
# Read-only; uses local user-secrets and ignored settings, prints no credentials.
dotnet run --project tools/IdentityAdmin --configuration Release -- inspect --environment Development --configured-admin

# Explicit Development-only activation, only for an already administrative,
# verified account on loopback SQL. Writes ignored local settings, not SQL rows.
dotnet run --project tools/IdentityAdmin --configuration Release -- enable-local-management --environment Development --configured-admin --approve-local-management

# Start/restart the API and independent management frontend.
dotnet run --project be --configuration Release --launch-profile be
npm --prefix client run dev:admin -- --host localhost --strictPort
```

Private local entry: `http://localhost:5179/Amaara-Creations/login`. API: `http://localhost:5192/api/v1`. Management activation was executed locally, leaves bootstrap disabled and preserves other ignored settings. It adds exact loopback management origins to the general CORS list and a separate management-only allowlist. Source/production defaults remain closed.

Sign in with the existing configured local credentials. Leave the authenticator field blank only for initial enrollment. The management app then opens security setup; operational data remains denied. Reconfirm the password, add the displayed key privately to the owner's authenticator, confirm its current code, store the ten recovery codes privately, then sign in with a **new** code. Codes are single-use; a confirmation code must not be replayed. The owner must perform this real-device step; tests never enroll or expose a seed for the actual local account.

If a different explicitly approved non-production identity lacks a role, the operator tool provides an idempotent alternative to direct SQL or global bootstrap:

```powershell
dotnet run --project tools/IdentityAdmin --configuration Release -- assign-role --environment Development --configured-admin --role Admin --approve-role-change --reason "Owner-approved local membership correction"
```

This command was **not run against the existing account**. It refuses Production, absent approval, unknown role names, absent/unverified accounts and pending migrations. It uses UserManager/RoleManager, normalizes membership through Identity, records an audit reason, rotates the security stamp and revokes sessions if membership changes. A repeated identical assignment returns unchanged without duplicate membership or audit entries. Staging requires selecting its configuration explicitly and the same approval flag. Use an already authorized, MFA-verified owner workflow or separately reviewed operator process for production changes.

Two earlier additive migrations are pending in the actual local database: `BoutiqueCartVariants` and `BoutiqueCustomerRequests`. They were not applied by this task. Authentication/security setup can be exercised independently, but complete commerce/request workflows in that database require the existing backup, migration review and cutover process. No database reset or destructive SQL is proposed.

## Deployed environment finding and build safeguard

A credential-free HTTP probe returned **200** for `https://g-thuva.github.io/Amaara-Creations/`. Its deployed entry asset `assets/index-BilhwsCC.js` declares **`http://localhost:5192/api/v1`**. This addresses a visitor's own computer, not a deployed API, and cannot establish a genuine public backend session for ordinary visitors. No local credentials were sent there. The web-reader tool was unavailable; the finding comes from read-only HTTP retrieval of the public HTML and public entry asset.

The Pages workflow now requires the public repository variables `VITE_API_BASE_URL`, `VITE_MEDIA_BASE_URL` and `VITE_PUBLIC_SITE_URL`, with explicit safe base/hash routing. `scripts/check-pages-config.py` rejects missing, localhost/private, example, non-HTTPS, credential-bearing and static-Pages-as-API configurations before installation/build/upload. Customer artifact isolation runs after building. This prevents a future build from silently reproducing the known bad configuration; it does not repair the currently deployed asset or verify a merchant-owned API by string validation alone.

No deployment, repository variable update, push or production login was performed. A real approved HTTPS API, same-site cookie/BFF strategy, narrow CORS/management origin lists and separately protected management host are still required. GitHub Pages hosts only the storefront. Public login must never redirect to the private management origin. The entire staff artifact must remain behind the approved identity-aware host if unauthenticated asset downloads must be denied.

## Verification record

| Check | Observed result |
| --- | --- |
| Backend / real SQL | **329 passed, zero failures/skips (28s)**, including provisioning/revocation transaction behavior, MFA and management-origin rejection |
| Frontend | 136 unit tests passed in 24 files; lint and TypeScript passed |
| Full development browser | 93 passed (6.3m), including actual Identity/JWT dashboard routing, restoration, separate cookies/logout, role denials, MFA enrollment and existing commerce/staff regressions |
| Production browser | **29 passed (1.8m)**, including actual MFA enrollment, dashboard restoration, pre-verification module isolation and a browser attempt to exchange management cookies from the approved storefront origin |
| Dashboard data | A further **7 management cases passed in development (39.2s) and production (33.7s)** after strengthening assertions to wait for actual authorized dashboard statistics and no error alert, including after refresh/restored sign-in |
| EF | No pending model changes; no new migration |
| Release/config tools | 14 Python tests passed, including bad Pages configuration rejection |
| Customer artifact | Isolation passed: 37 manifest entries, 38 assets; no staff modules/maps. Initial JS 115,868/128,000 gzip bytes, largest route 8,324/40,000, CSS 76,337/100,000 bytes |
| Operator tool | Release build passed, read-only actual identity inspection passed, local-only activation passed; production role mutation refused in tests |
| Secret scan | **870 text files, zero candidates** on final source; private/generated configuration excluded from commits and verified absent from operator output |
| Dependency audits | npm zero vulnerabilities; NuGet zero known vulnerable packages from configured feeds; no new package dependency |
| Deployed configuration | Public asset read-only inspected; localhost API defect confirmed; genuine deployed authentication not verified |

The first broad run found existing business-test principals and two old UI/variant setups still using the shared-login contract; these were updated to represent the management boundary. No authorization check or test was disabled. Existing mocked payment-state fixtures remain explicitly UI-only and are not authentication evidence. New authentication journeys call the real Identity/JWT endpoints with disposable seeded users. Delayed transport tests delay real responses rather than fabricate successful logins.

The initial operator build inherited private appsettings copy behavior. The secret scanner rejected the generated copy without printing values. Private development settings are now excluded from both normal build output and publish output; generated tool copies were removed and the rebuilt output verified. Standard tool bin/obj output is ignored; source and packaged artifacts remain scanned. No credentials were committed or transmitted.

Remaining limits: actual owner-device MFA enrollment, backed-up application of the two pre-existing local migrations, genuine production API/host/cookie verification, staff gateway provisioning and deployment approval. Local automated checks do not establish deployed security or manual accessibility certification. All changes remain on the focused local branch until separately authorized.
