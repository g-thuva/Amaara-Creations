# Authentication, sessions and password policy

## New credentials and existing accounts

Continue ASP.NET Core Identity hashing and existing stored hashes. New registration, reset and change-password operations validate 15–128 Unicode code points with a 256 UTF-16-unit API bound. No uppercase/digit/punctuation composition rule, forced periodic rotation or paste blocking is added. Existing shorter passwords can still sign in, avoiding an account lockout migration. A local whole-password blocklist and configurable `Authentication:BlockedPasswords` reject common choices; this is not comprehensive breach-corpus verification. NFC is used for blocklist comparison, not a silent rehash/normalization migration of existing passwords.

Password visibility is local display state only. Every customer password field and the staff sign-in use PasswordField; inputs remain masked by default with current/new autocomplete, a keyboard-operable visibility button, Caps Lock hint and a 30-second/window-blur rehide. Guidance is length-based and does not claim to measure password entropy or confirm breach status.

Duplicate registration now uses the same successful response as a new registration. Login errors remain generic except the existing email-verification guidance after a correct password. Forgot/reset flows preserve verified expiring Identity token processing. Data-protection reset/verification tokens expire after 30 minutes; password reset/change updates the stamp and revokes refresh sessions.

## Sessions and revocation

Access JWTs remain in memory, not new localStorage/sessionStorage token persistence. Refresh tokens are random, hashed server-side, rotated, replay-detected and sent in HttpOnly cookies. Production cookies are Secure, SameSite=Lax and scoped to `/api/v1/auth`. Cookie-bearing authentication POSTs reject untrusted Origin values and cross-site browser requests with no Origin; approved origins are exact configuration values. This design requires same-site frontend/API deployment for the Lax cookie flow.

Every access token carries the user's current security stamp and a refresh-session family. Bearer validation checks signature/HS256, JWT token type, issuer, audience, expiry, stamp, account availability and an unrevoked/unexpired family, then replaces claimed roles with current database roles. Logout revokes the family; logout-all changes the stamp and revokes all families. Existing pre-change access tokens lack binding and must sign in again after rollout.

## Staff assurance

Staff APIs are closed unless `Administration:Enabled=true`. A correct enrolled Identity authenticator code adds MFA assurance to the server-minted token. Incorrect factors increment the same account lockout counter without a correct password resetting it first. Rate limiting applies to login.

A separate data-protected HttpOnly assurance cookie contains only user/stamp/family/original verification time. Refresh accepts it only for that same active family/user/stamp and within 15 minutes. Rotation preserves the original time; it never slides the MFA window. Sensitive policies expire at 5 minutes. These fixed maximum lifetimes also bound inactivity; there is no long-lived privileged session renewal. The staff shell verifies capabilities before importing operational modules.

Remaining release requirements: audited MFA enrollment/reset/recovery and authenticator-code replay prevention, phishing-resistant passkeys where feasible, account security notifications, complete session management and customer MFA, configured blocklist operations and a manual recovery review. Administrative rollout stays disabled until those requirements are resolved. Disposable TOTP fixture endpoints exist only in the separate acceptance executable.
