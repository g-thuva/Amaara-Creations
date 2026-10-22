# Administrator sign-in diagnosis

Starting point: `develop` at `0e3d890`; focused branch `fix/admin-auth-dashboard`. The observations below record the original diagnosis. The owner subsequently approved redirecting successful storefront staff logins to the separate management sign-in. The applications still use independent authentication sessions.

## Current post-login behavior

The original customer hook ignored the authenticated response's roles and always selected a safe shopping destination, usually `/profile`. The first correction redirected staff to a separate sign-in but required entering credentials twice. The owner then selected **email, password and authenticator code on one form**. The storefront now uses `/api/v1/auth/sign-in`: the server validates these once and current role membership determines whether it also prepares an independent management session. `loginDestination` routes staff to the separate management application; customers retain shopping returns. No email is hardcoded and browser storage is not consulted. Wrong credentials do not redirect.

Local development defaults to the current loopback hostname, port 5179 and the application base path: `http://localhost:5179/Amaara-Creations/login`. Production requires the approved HTTPS `VITE_MANAGEMENT_LOGIN_URL` on a separate origin. Credentials, tokens, return URLs and identity fields are never appended or posted to that host. Missing or unsafe configuration produces an explicit staff sign-in configuration error; the Pages guard also requires this setting. The management destination is public build configuration and is discoverable; anonymous navigation still has no admin link and the storefront still excludes operational modules.

The new shared sign-in creates separate shopping and management refresh-session families after the same verified credentials/factor, preserving the original MFA verification time. Only the shopping access token appears in its JSON response. The management refresh secret and assurance use the existing protected HttpOnly staff-scoped cookies, read only through the approved management origin. The destination restores this session and opens the permitted dashboard without a second password or authenticator code. The legacy `/auth/login` remains shopping-only; direct staff sign-in remains available. This is intentionally a shared credential entry with separate operational sessions, replacing the previous independent-login boundary.

An unenrolled account restores initial authenticator setup without a second login, but cannot use operations until its owner completes enrollment. Recovery-only sessions permit repair, not operations. Disabled management and invalid credentials/factors cannot issue staff sessions. Role changes, security stamps, revocation, assurance expiry and origin restrictions remain enforced by the backend. No schema, existing account password, membership or MFA enrollment was changed. Ordinary subsequent successful sign-ins create session/audit records as usual; verification uses disposable in-memory accounts.

## Confirmed observations

| Finding | Evidence | Consequence |
| --- | --- | --- |
| Original public login always returned to a shopping destination | `client/src/features/auth/hooks/useLogin.js` previously ignored response roles; public `main.jsx` never imports staff routes | This caused the reported `/profile` result for staff. The owner now explicitly authorizes navigation to the separate management sign-in after successful staff authentication |
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

Keep customer login destinations customer-facing and keep operational staff code out of its artifact. Redirect successfully authenticated staff to the separate management sign-in as now approved by the owner. Give management dedicated server-selected login/refresh/logout paths and protected, separately named cookies; staff authorization must require a management session in addition to current roles and MFA. Provide authoritative management state, explicit enrollment/recovery/denial/expiry handling and permission-aware local return destinations. Prevent stale asynchronous authentication from overwriting newer operations. Retain current roles, pricing, data, design system and all operational policies. Supply read-only Identity inspection and an explicit, idempotent, non-production provisioning command; do not promote the existing account.

Local activation and genuine MFA enrollment are separate steps: the owner enters existing credentials privately and enrolls their own authenticator. No TOTP bypass, fabricated administrator credentials or automatic enrollment is allowed. Production administration remains disabled until its protected host and operational approvals are supplied.

## Reproduction and verification plan

1. On the storefront login, authenticate a disposable staff account once: create independently scoped sessions through server-verified roles/MFA, navigate without credentials/tokens in the URL, and restore the management workspace without a second login. Customer logins remain on safe shopping destinations; invalid credentials and forged browser roles cannot trigger the staff redirect.
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
- `returnDestination`, `loginDestination`, `useLogin`, `AuthGuard`, permissions: validated customer return destinations reject external, protocol-relative, backslash, encoded slash, control-character and login-loop values. Successful staff logins navigate only to the separate configured sign-in. No management menu is added to storefront navigation, footer, help or sitemap; management paths and credentials are not carried from customer return state.

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

A concrete private COPY_ONLY/checksum backup and count-preserving local migration procedure was prepared. Automatic approval review rejected execution because the authentication request did not explicitly authorize database duplication, migration or downtime. The owner subsequently selected **Leave database unchanged**. That rejected command did not run: no backup or migration was performed. This is an operational compatibility gate, not missing administrative role membership.

## Deployed environment finding and build safeguard

A credential-free HTTP probe returned **200** for `https://g-thuva.github.io/Amaara-Creations/`. Its deployed entry asset `assets/index-BilhwsCC.js` declares **`http://localhost:5192/api/v1`**. This addresses a visitor's own computer, not a deployed API, and cannot establish a genuine public backend session for ordinary visitors. No local credentials were sent there. The web-reader tool was unavailable; the finding comes from read-only HTTP retrieval of the public HTML and public entry asset.

The Pages workflow now requires the public repository variables `VITE_API_BASE_URL`, `VITE_MEDIA_BASE_URL`, `VITE_PUBLIC_SITE_URL` and `VITE_MANAGEMENT_LOGIN_URL`, with explicit safe base/hash routing. `scripts/check-pages-config.py` rejects missing, localhost/private, example, non-HTTPS, credential-bearing and static-Pages-as-API configurations before installation/build/upload. Customer artifact isolation runs after building. This prevents a future build from silently reproducing the known bad configuration; it does not repair the currently deployed asset or verify a merchant-owned API by string validation alone.

No deployment, repository variable update, push or production login was performed. A real approved HTTPS API, same-site cookie/BFF strategy, narrow CORS/management origin lists and separately protected management host are still required. GitHub Pages hosts only the storefront. Successful staff logins may now navigate to the configured management sign-in as explicitly approved; they cannot exchange management cookies from the storefront. The entire staff artifact must remain behind the approved identity-aware host if unauthenticated asset downloads must be denied.

## Single-form sign-in verification

- Backend: **42 focused tests passed**, including nine new shared-sign-in cases, management authentication, MFA journeys, staff authorization and API contract checks. They verify one consumed authenticator code establishes separate session families with the same assurance time; the storefront receives no management bearer token; invalid credentials, hostile origins, disabled management and posted roles cannot grant access. Recovery and unenrolled accounts remain limited to repair/setup. Tests use disposable in-memory accounts, not the business database.
- Frontend: **166 unit tests passed**; lint and TypeScript passed. Authenticator entry is visible on the same form; recovery is a secondary option. The HTTP retry guard recognizes the new sign-in endpoint.
- Development browser: 17 cases passed initially; the renamed recovery disclosure required updating one old locator. Both customer security cases then passed on rerun (19.0s), completing coverage of the 18 selected cases.
- Production browser: **all 18 cases passed (1.8m)**. The handoff test proves exactly one shared credential submission and zero additional staff login submissions before the real management dashboard, actual statistics and successful reload. Customer lifecycle, MFA enrollment/recovery, cookies, role denials and management-origin checks also passed. The HTTPS deployment destination is intercepted, then the test loads the actual loopback management app using the issued cookie; this does not assert an external deployment exists.
- Storefront artifact: operational-module isolation passed (37 manifest entries/38 assets); initial JS 115,878/128,000 gzip bytes, largest route 8,321/40,000, CSS 76,337/100,000 bytes.
- Local application: restarted only the verified Development API with the tested build and restored the agent-owned management frontend. Browser smoke confirms all three single-form fields visible on the storefront, the expected loopback destination, the new endpoint's input validation, healthy Development API, zero page errors and unchanged management-origin restrictions. No real credentials were submitted. No database backup, migration, password, role or authenticator enrollment change was performed. The local API is running; reload the storefront to use the new form.

## Earlier storefront redirect follow-up verification

This section records the earlier redirect-only version. The single-form handoff supersedes its no-management-session expectation; current verification is recorded above.

- Frontend: **166 unit tests passed** in 25 files, including all seven staff roles, multiple memberships, customer return paths, missing/unsafe destinations and credential-free navigation; lint and TypeScript passed.
- Browser: **14 development cases passed (1.0m)** and **14 production-build cases passed (43.5s)**. These cover actual Identity authentication with disposable in-memory accounts, staff redirects, customer and invalid-credential behavior, forged browser roles, restoration/logout races, independent management cookies, MFA, specialist permissions and storefront-origin denials. The redirect destination uses an intercepted HTTPS test host to verify the exact credential-free navigation; the separate staff project tests the real management application and API.
- Pages configuration: **5 tests passed**, requiring an approved HTTPS management destination on a different origin, including rejection of an equivalent explicit `:443` storefront origin.
- Production artifact: isolation passed with 37 manifest entries/38 assets; initial JS 115,872/128,000 gzip bytes, largest route 8,322/40,000, CSS 76,337/100,000 bytes.
- Actual local management entry: browser smoke passed after restoring the agent-owned frontend; API origin 5192, anonymous refresh 401, management-origin validation 400, storefront-origin denial 403 and zero page errors. The user's API remained running. No real credentials were submitted and the business database was not changed.

## Verification record

The following records the earlier comprehensive authentication work, before the storefront redirect follow-up above.

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
| Clean dependency/package build | **Passed**, seven steps including fresh isolated npm installation, .NET restore/publish, EF consistency and artifact privacy/checksums; 130 files from code commit `556da40` |
| Published API rehearsal | **Passed** over local HTTPS against fresh disposable SQL, with genuine JWT/MFA staff reads, public/private media, paused-payment behavior and browser smoke; no production deployment |
| Actual local app | **Passed** after restarting the verified workspace API and starting management on loopback: API liveness 200/Development, management HTML 200, anonymous staff session 401, correct API origin 5192, approved management-origin invalid input 400, storefront-origin exchange 403, zero browser page errors |
| Deployed configuration | Public asset read-only inspected; localhost API defect confirmed; genuine deployed authentication not verified |

The first broad run found existing business-test principals and two old UI/variant setups still using the shared-login contract; these were updated to represent the management boundary. No authorization check or test was disabled. Existing mocked payment-state fixtures remain explicitly UI-only and are not authentication evidence. New authentication journeys call the real Identity/JWT endpoints with disposable seeded users. Delayed transport tests delay real responses rather than fabricate successful logins.

The initial operator build inherited private appsettings copy behavior. The secret scanner rejected the generated copy without printing values. Private development settings are now excluded from both normal build output and publish output; generated tool copies were removed and the rebuilt output verified. Standard tool bin/obj output is ignored; source and packaged artifacts remain scanned. No credentials were committed or transmitted.

Remaining limits: actual owner-device MFA enrollment, backed-up application of the two pre-existing local migrations, genuine production API/host/cookie verification, staff gateway provisioning and deployment approval. Local automated checks do not establish deployed security or manual accessibility certification. All changes remain on the focused local branch until separately authorized.

## Final local handoff

Code commit: `556da40` on `fix/admin-auth-dashboard`, based on `develop` at `0e3d890`. The clean checkout and verified package are retained at `.runtime/admin-auth-release-checkout` and `.runtime/admin-auth-release-checkout/.runtime/releases/admin-auth-556da40`. Later documentation-only commits do not change that application source. Logs remain private under `.runtime/admin-auth-*`; no credentials, seeds or recovery codes are committed. The branch was not pushed or merged.

The previous API process was verified as the workspace's ordinary Debug development run on loopback port 5192, then replaced with the corrected Release build running in Development. Management now listens only on localhost:5179. The existing storefront dev process was left running. Process identifiers and private logs are recorded under `.runtime/admin-auth-local-processes.json`. No migration or account mutation was performed as part of startup/activation; actual owner MFA enrollment is deliberately left to the owner's device.

Reviewable file groups: backend `StaffAuthController`, `AuthController`, JWT/cookie/origin/policy adapters, Identity service/provisioner and session transaction ownership; frontend `admin-main`, `StaffApp`, management API, shared authentication lifecycle/transport and safe return paths; operator CLI under `tools/IdentityAdmin`; Pages public-config guard/workflow; corresponding API/SQL/unit/browser/config tests and compatibility fixture; private-build-output exclusions and this security documentation. Use `git show --stat 556da40` for the exact file list. No package version, historical migration or database model changed.

Production must supply an approved HTTPS API and media/canonical URLs through public build variables, exact general CORS plus management-only origin lists, same-site cookie hosting or an approved BFF, protected management assets and durable protected Data Protection keys. Validate authenticated production restoration, MFA, revocation and denied asset requests in staging before requesting deployment approval. The currently deployed localhost-based Pages asset remains an explicitly identified external deployment issue.
