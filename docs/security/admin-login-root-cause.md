# Administrator sign-in diagnosis

The original `/profile` redirect came from `useLogin`: it ignored the authenticated user's roles and always chose a customer destination. A read-only inspection of the configured local SQL identity confirmed both `Admin` and `SuperAdmin` memberships, a confirmed email and no completed MFA. Missing roles were not the cause. No existing password, role, enrollment or business record was changed to correct navigation.

The first redirect fix required another staff login. The next single-form version exposed an optional authenticator field to everyone and still sent first-time staff through manual setup, repeated password entry, logout and another code. The current implementation consolidates these into the shared progressive flow requested in the latest specification.

## Current behavior

- Customer: email/password → validated shopping destination, normally `/profile`.
- New staff/admin: email/password → stable QR/manual setup → verified code → save recovery codes → permitted workspace.
- Returning staff/admin: email/password → current authenticator code → permitted workspace.
- Lost-phone recovery: verified saved code or another freshly verified owner's approval → replacement QR verification and recovery-code acknowledgment → permitted workspace.

Existing customers who deliberately enabled optional MFA retain it. Ordinary customers do not see setup or a challenge. A current privileged membership always triggers required MFA; unknown account roles fail closed. Existing specialist role names are retained, and `SuperAdmin` is the owner role.

`POST /api/v1/auth/sign-in` performs first-factor verification. Pending staff receive an opaque HttpOnly challenge cookie and a protected expiring Identity token record, **no ordinary JWT and no refresh session**. The next step checks the original nonce, stamp, current roles, expiry and lockout. Native Identity TOTP verification and SQL per-user locks prevent bypass and replay. Enrollment changes the active key only after verification, atomically revokes prior sessions and rotates the stamp. Saved-code acknowledgment completes sign-in without another password or code.

Only completed authentication creates independent shopping/staff session families. The storefront receives its shopping token and navigates to the configured separate management application; management restores its staff-scoped cookies and checks authoritative permissions before importing operational routes. Its entry has no second login form. Management tokens, refresh exchanges, current roles and recent MFA remain enforced by the API. A shopping JWT containing staff roles cannot authorize management operations.

For details use the [audit](authentication-mfa-audit.md), [architecture](mfa-architecture.md), [phone setup guide](staff-mfa-setup.md), [recovery procedure](mfa-recovery.md), and [current verification](mfa-testing.md). Earlier single-form/redirect-only test counts describe previous commits and are superseded by that verification record.

## Local operation

Current work is local on `fix/admin-auth-dashboard`, based on `develop` at `0e3d890`. The earlier boutique feature was already merged to develop and deleted; this focused authentication branch has not been pushed, merged or deployed.

Use `http://localhost:5173/Amaara-Creations/login`. Management runs at `http://localhost:5179/Amaara-Creations/login` and the configured API at `http://localhost:5192`. Use one consistent loopback hostname. The server checks current memberships independently of public navigation, editable profile data and browser storage. The actual account owner must enroll their own phone privately; tests use synthetic users only.

Private Development configuration already enables administration, keeps bootstrap disabled and supplies existing credentials/SQL/JWT settings. Do not print those settings. The read-only operator inspection is available without mutating accounts:

```powershell
dotnet run --project tools/IdentityAdmin --configuration Release -- inspect --environment Development --configured-admin
```

The owner's instruction is **Leave database unchanged**. No backup, migration, role assignment, password change, MFA reset or actual-account enrollment was performed. Two pre-existing additive business migrations remain pending: `BoutiqueCartVariants` and `BoutiqueCustomerRequests`. They affect broader commerce/request readiness and require their own reviewed operational process, not an authentication workaround. No new schema migration is needed for this refactor.

## Production boundary

A previous credential-free inspection found the deployed GitHub Pages asset pointed at `http://localhost:5192/api/v1`. That addresses the visitor's computer and does not establish a working public backend. This is a historical observation; the current task does not claim that deployment has since been repaired or tested.

The Pages build guard requires approved public HTTPS API/media/site/management URLs and rejects loopback, private, example and unsafe destinations. This prevents another misconfigured build; it does not deploy an API. Production also needs exact CORS plus management-only origins, reviewed same-site cookie/BFF hosting, protected management assets, accurate server time and durable protected Data Protection keys. See [deployment and compatible rollback](ADMIN_ISOLATION_DEPLOYMENT.md). Physical Google Authenticator device verification and real production hosting remain owner/operator acceptance steps.
