# ASVS 5.0.0 verification mapping

Target: applicable ASVS Level 2 controls, using the [official 5.0.0 standard](https://asvs.owasp.org/). This is a scoped engineering mapping, not a complete numbered control audit or certification. Exact per-requirement applicability and independent review remain release work.

| Control area | Existing / changed evidence | Verification and residual gap |
| --- | --- | --- |
| Authentication | Identity hashing, confirmed email, generic login/recovery, new passphrase validator, lockout/rate limiting, staff TOTP | New policy/password/MFA HTTP tests; broad compromised corpus, replay-safe MFA recovery and security notifications still incomplete |
| Session management | Rotating hashed refresh families, HttpOnly/Secure/Lax cookies, stamp/family-bound JWTs, fixed MFA assurance and logout invalidation | Logout, rotation, replay and SQL concurrency tests; inspect real proxy/cookie/key-ring configuration before release |
| Authorization | Explicit AppPolicies roles, database-refreshed claims, object-scoped services, MFA on every staff policy, isolated staff build | Existing architecture/API/ownership tests plus new negative cases; independent exhaustive endpoint/object review still required |
| Input / business logic | Typed contracts, bounded catalogue pagination, configured option validation, versioned quotes, transaction/idempotency controls | Existing pricing, checkout, coupon, stock, refund and webhook suites; no browser-supplied amount authority |
| Files and resources | Signature/extension/MIME and size/pixel/structure validation, random filenames, private artwork/proofs and authorized downloads | Existing malformed/corrupt/MIME/ownership browser/backend suites; full safe image decode/re-encode, metadata removal and malware scanning are not established by structural checks and remain release gaps |
| Browser / frontend | Scoped design system, named controls, protected route behavior, no customer staff imports, no source maps | Emitted-artifact inspection and browser negative paths; true static-asset confidentiality requires the protected staff host |
| Data / privacy | Existing owner-scoped read/write, audit redaction and currency-qualified financial history | No customer data altered; operator retention, export/deletion and policy processes need implementation/legal review |
| API / communications | JWT validation/algorithm allowlist, exact CORS, auth-origin checks, HTTPS/HSTS templates, restrictive API headers | Auth CSRF negative tests and headers checks; host enforcement and exact production origins remain unverified |
| Logging / monitoring | Correlation IDs, redacted structured telemetry, existing append-only audit and protected mutations | Existing audit/rollback tests; review deployed append-only SQL permissions, retention and abnormal sign-in alerting |
| Configuration / dependencies | Bootstrap/provider defaults disabled, no client secrets, scan/audit tooling, migration integrity checks | Local scans and package feed checks reported separately; production secrets, SBOM/history review and encrypted key/backup custody remain operational gates |

Go/no-go: no production approval while critical gaps remain. In particular, successful in-memory tests do not prove SQL races; a successful local restore rehearsal does not prove encrypted production backups; successful axe checks do not prove screen-reader usability; a separate public build does not conceal staff assets.
