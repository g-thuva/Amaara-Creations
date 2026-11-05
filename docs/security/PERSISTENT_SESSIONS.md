# Persistent authentication: implementation and verification

Current account entry uses the same login/signup pages for customers and staff. Privileged identities additionally complete mandatory MFA and receive only management sessions. Challenges remain bound to their entry, and customer and management refresh cookies retain separate scopes. The lifetime policies below remain unchanged; see the current [shared login and staff MFA implementation](SHARED_LOGIN_AND_STAFF_MFA.md).
## A. Confirmed causes

The existing application already had in-memory access JWTs, persistent HttpOnly refresh cookies, hashed SQL refresh-session families, rotation, origin checks, current-role validation and guided MFA. Replacing that infrastructure was unnecessary.

Privileged assurance cookies and refresh validation expired after a fixed fifteen minutes. Normal staff authorization used the same limit, so a valid remembered browser session could not outlive that assurance window. Frontend bootstrap treated every refresh failure as signed out, and the HTTP interceptor cleared identity even for network failures, rate limits and server outages. Refresh coordination covered one tab only; a second exchange of a recently rotated credential revoked the winning family. Rotation also restarted the global 21-day refresh lifetime indefinitely, without separate meaningful-idle or absolute limits.

The later live MFA `500` on port 5192 had an additional confirmed deployment cause: the API exposed the new session endpoint while `20261102192511_PersistentSessionPolicy` was pending in its development database. MFA session issuance needed the missing columns. The additive migration has now been applied to that configured development database. The API was restarted with the validated Release build in Development; `/health/ready` returns `{"status":"ready"}`, and the migration list has no pending entries. Storefront 5179 and admin 5173 also return HTTP 200. API logs are `.runtime/persistent-auth-api.stdout.log` and `.runtime/persistent-auth-api.stderr.log`. A previously consumed challenge requires a fresh credential/MFA sign-in. No production deployment was performed.

`401` remains the correct response for absent/invalid credentials or an expired challenge. The unrelated `content/pages/home` and `about` responses are `404` when no published CMS record exists; the existing frontend already treats those optional pages as missing content. No fabricated CMS content was inserted.

## B. Existing and improved architecture

| Reused | Improvement |
| --- | --- |
| ASP.NET Core Identity, existing JWT issuer/audience/signing/claims | Short access lifetimes, capped by the original absolute session deadline |
| `RefreshSessions`, SQL per-user application locks | Add policy timestamps, role/stamp fingerprints, session scope and MFA binding to the same table |
| Rotating 64-byte random refresh credentials and SHA-256 hashes | Rotation preserves family creation, absolute deadline, idle deadline and original MFA time |
| HttpOnly shopping and protected management cookies | Persistent session-limited assurance cookies; configurable reviewed SameSite policy |
| Axios and `AuthProvider` | Coordinated renewal, recoverable bootstrap, safe retries, lifecycle signals and current server identity updates |
| Guided staff MFA and existing permission assignments | Session-bound MFA remains mandatory; five-minute sensitive-operation checks remain intact |

## C. Implemented lifetime configuration

These are repository defaults verified by API tests, not proposed settings. Environment configuration may override them.

| Policy | Customer | Privileged management session |
| --- | --- | --- |
| Access JWT | 15 minutes | 10 minutes |
| Meaningful idle limit | 7 days | 30 minutes |
| Absolute family limit | 30 days | 12 hours |
| Refresh credential expiry | Original absolute deadline | Original absolute deadline |
| Persistent refresh/assurance cookie | Explicit absolute expiry | Explicit absolute expiry |
| Sensitive staff authorization | Existing account-security checks | Original MFA must be less than 5 minutes old |

Keys are `Authentication:Sessions:CustomerAccessMinutes`, `CustomerIdleMinutes`, `CustomerAbsoluteMinutes`, `PrivilegedAccessMinutes`, `PrivilegedIdleMinutes` and `PrivilegedAbsoluteMinutes`. Existing shorter `Authentication:AccessTokenMinutes` / `Jwt:AccessTokenMinutes` values remain a cap during rollout. Startup rejects nonpositive limits, effective access lifetimes over thirty minutes, idle limits exceeding absolute limits, and absolute limits exceeding a year.

The login page clearly informs users that the browser remembers the permitted server session. There is no password storage or optional checkbox that falsely implies browser closure revokes a session.

Only explicit authenticated `POST /api/v1/auth/activity` records activity. The frontend sends this for trusted pointer/keyboard input in a visible document, at most once per minute. SQL uses server time and the JWT-bound user/family; client timestamps are ignored. Silent refresh, capability checks, ordinary API reads, polling and timers never renew idle time. Expiration and revocation are checked on every protected API request and renewal; an unattended page may remain displayed until its next session check, but its expired credentials cannot authorize further requests.

## D. Important files

| Files | Responsibility |
| --- | --- |
| `be/Domain/Identity/RefreshSession.cs`, `Application/Features/Identity/SessionPolicy.cs` | Persistent policy state and validated limits |
| `be/Infrastructure/Identity/RefreshSessionService.cs`, `TokenService.cs` | Atomic rotation, replay/conflict handling, activity, revocation and access expiry |
| `be/Application/Features/Identity/IdentityService.cs`, `AccountSessionService.cs` | Verified issuance/restoration and active-device filtering |
| `be/Api/Identity/RefreshCookie.cs`, `StaffAuthorization.cs` | Cookie scope/security and ordinary versus sensitive MFA authorization |
| Auth controllers, authentication/CORS extensions, `AuthOriginMiddleware.cs` | Session activity, authoritative verification, safe retry marker and CSRF/CORS boundaries |
| `be/Infrastructure/Persistence/Migrations/20261102192511_PersistentSessionPolicy*`, current snapshot | Ten additive columns on the existing table; no account/business-data deletion |
| `client/src/shared/api/httpClient.js`, `authSession.js`, `sessionEvents.js` | One renewal per request group/tab lock, generation guards and credential-free lifecycle signals |
| `AuthProvider.jsx`, guards, login components, `useSessionActivity.js`, `StaffApp.jsx` | Bootstrap, recovery/error UX, current roles, stricter session activity and existing redirects |
| `client/src/shared/hooks/useResource.js` | Clear disabled user resources; identity changes also remount authenticated application state |
| New session HTTP/event unit tests, `session-lifecycle.spec.ts`, backend session/cookie/policy tests | Regression coverage |
| `tests/be.AcceptanceHost/Program.cs` | Isolated expiry/revocation/expired-JWT controls, absent from the deployable API |

## E. Security controls and migration

SQL-loaded UTC expiry values are explicitly normalized before writing cookies or clamping JWT expiry. A regression test verifies that a timestamp loaded without a .NET `DateTimeKind` does not shift into the machine's local time zone.

SQL locks serialize rotation/logout/activity across API instances. A duplicate exchange within ten seconds receives `409 SESSION_REFRESH_CONFLICT`, no token and no cookie update. It does not revoke the winner. The client serializes same-origin tabs using Web Locks and retries such a conflict once using the shared cookie jar. The previous credential never issues a second access or refresh token. Replay outside that bounded interval revokes the family. Logout with a superseded credential revokes its current replacement, covering refresh/logout races.

Cookies are host-only, HttpOnly and limited to `/api/v1/auth` or `/api/v1/auth/staff`. Production always sets Secure; SameSite defaults to Lax. `Authentication:CookieSameSite=None` requires HTTPS/Secure, including explicit `Authentication:SecureCookies=true` for HTTPS development. This does not bypass browsers' third-party-cookie restrictions. Prefer the existing reviewed same-site/BFF deployment topology. Narrow cookie paths are incompatible with the `__Host-` prefix's root-path requirement, so the existing independently scoped names remain.

Exact approved credentialed origins, management-specific origin restrictions and Origin/Fetch Metadata rejection protect cookie mutations. Bearer operations retain backend authorization. Only a `401` carrying the authentication middleware's `X-Auth-Refresh: required` marker can retry once: that middleware rejects the request before endpoint side effects. Business `401`s, all original `403`s and ambiguous network failures are never replayed. A refresh `403` does not clear a valid session as though it expired.

Server state binds access and refresh credentials to the family, account security stamp, current roles, application scope and original MFA verification time. Password/security changes, role changes, lockout, revocation and expiration fail closed. Ordinary staff access uses its valid bounded session; sensitive policies still require recent MFA. Initial privileged cookies are issued only after required MFA completes. No refresh credential, password, TOTP secret or recovery code is persisted in frontend storage or cross-tab messages. Logout clears local identity only after confirmed server revocation; a network failure instead displays an honest retry state and removes protected views.

Migration SQL was inspected before application: ten `ALTER TABLE RefreshSessions ADD` statements plus migration history, inside a transaction. Historical migrations remain unchanged. The current model snapshot and its fingerprint were updated together; the new migration payloads are also fingerprinted. Old sessions without policy/stamp/scope metadata deliberately require one new sign-in. Accounts, passwords, MFA enrollment, roles, orders and payment history are preserved. Other environments must apply this migration before exposing the updated API and retain durable Data Protection keys.

## F. Verification

Node 22 and the existing test frameworks were used. Authentication/browser tests used real backend authentication and isolated users. SQL concurrency/migration tests used guarded `AmaaraPhase6Tests_*` databases. Existing business data was not reset. The explicitly requested live-error fix applied only the inspected additive migration to the configured development database.

| Check | Actual result |
| --- | --- |
| `npm.cmd test -- --reporter=dot` | 291 tests passed, 37 files |
| `npm.cmd run typecheck` | Passed |
| ESLint over changed/new JS and JSX | Passed |
| `npm.cmd run build`, `npm.cmd run build:admin` | Passed |
| `npm.cmd run check:isolation` | Passed, 40 storefront manifest entries / 47 assets |
| Backend Release builds, isolated acceptance host | Passed, zero warnings/errors in the final builds |
| `npx.cmd playwright test --config ../.runtime/session.playwright.config.ts --workers 1 --max-failures 3` | 19 passed: fourteen new session cases and five existing admin authentication cases |
| `dotnet test tests/be.Tests/be.Tests.csproj --configuration Release --artifacts-path .runtime/session-tests --no-restore` with guarded local test SQL | 466 passed / one pre-existing SQL checkout failure / zero skipped, 467 total. All authentication/security cases passed, including SQL timestamp normalization. Final evidence: `.runtime/session-test-results/session-final.trx` |
| Unchanged `1825399` SQL payment-workflow baseline | Reproduces the same pre-existing `409` at `PaymentWorkflowSqlTests.cs:52` |
| Live migration list and `/health/ready` on 5192 | Migration applied; no pending entries; `ready` |

Browser coverage includes full Chrome process/profile close-and-reopen for both customer and MFA admin accounts, concurrent tab reloads, cross-tab logout, real signed expired JWTs, one coordinated renewal, a cart mutation applied exactly once, preserved URL, server idle/absolute expiry/revocation, required MFA after expiration, initialization without protected/login flicker, temporary connectivity recovery and failed-logout retry. Expiry controls and expired-token issuance exist only in the nonpublishable acceptance executable. No fake authentication response was used as final verification.

## G. Remaining limits

The pre-existing real-SQL checkout/cart workflow failure is outside authentication and was reproduced in a clean baseline checkout; checkout/payment code was preserved. Existing full-repository frontend lint still has the unrelated Instagram unused-variable error and dropdown hook warning documented by the prior redesign work; changed authentication sources lint cleanly. NuGet vulnerability lookup was unavailable on the restricted network during the initial restore; cached dependencies compiled and tests ran, but this is not a completed dependency-vulnerability audit.

Production HTTPS/proxy/domain/cookie behavior and deployment were not exercised against a real hosted environment. Chrome was tested; other browser engines, physical mobile devices and manually entered production administrator credentials were not tested. A lost rotation response that never updates any tab's cookie cannot safely recover the superseded secret; reauthentication may be needed rather than reusing it. No production secrets were added, and no remote push was performed for this authentication task.

The expiration and cookie design was checked against [OWASP session-management guidance](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html).
