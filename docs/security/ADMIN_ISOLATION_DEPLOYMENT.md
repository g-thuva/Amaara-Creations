# Independent staff deployment and rollback

## Local architecture

`npm --prefix client run build` produces `client/dist` (storefront). `npm --prefix client run build:admin` produces `client/dist-admin` (staff). `check:isolation` inspects the customer manifest and emitted JavaScript for operational modules/clients and source maps. Both applications retain shared low-level presentation, identity and HTTP modules. Storefront routing never imports staff routes. Staff bootstrap has only sign-in until the backend capability request confirms current roles and MFA; operational routes then load lazily.

Development uses ports 5178/5179 in the browser harness, with API acceptance bridge 5198. Those are separate test origins, not a claim that a production deployment exists. The server defaults to `Administration:Enabled=false`. Existing staff operations will therefore be intentionally denied until properly provisioned and enabled.

## Production proposal — no deployment performed

Keep .NET API and SQL Server. Serve storefront and staff artifacts from separate approved origins. Preserve the existing headers-capable nginx/static deployment path or evaluate a managed static host; do not replace backend persistence. Review actual host pricing before selecting a paid plan; no host cost is quoted or approved here.

If staff JavaScript must be unavailable to unauthenticated users even by guessed asset URL, place **the entire staff artifact, including assets**, behind an identity-aware proxy/gateway. Separate Vite output and noindex alone cannot prevent someone downloading a publicly hosted asset. Backend role/MFA policies remain mandatory behind that gateway.

`deploy/staff-access-gateway.example.conf` is a concrete deny-only nginx boundary for this deployment. Its access subrequest returns 403 until the operator connects and verifies the approved identity provider. It covers HTML, deep links and assets and excludes maps/manifests. Provider assertions, group membership, MFA, unavailable-provider behavior and authorized/unauthorized asset requests must be tested before activation. It is not a deployed or working external identity-provider integration.

Required owner/operator work: domain/DNS/TLS approval; protected staff origin and gateway; exact CORS/API/media origins; same-site cookie compatibility; durable encrypted data-protection key storage/backup; deliberate staff enrollment/recovery; CSP with exact API/image/font origins; cache/no-store rules for private surfaces; deep-link fallback and normal unknown-page behavior; sandbox provider callback and reconciliation checks. Public sitemap must contain only published shop resources. Do not publish staff source maps or mix artifacts.

Existing `deploy/security-headers.conf.template` includes HSTS, CSP, nosniff, framing, referrer and permissions restrictions. Its style policy still permits inline styles because current dimension/color previews use them; nonce/hash hardening requires testing, not an unverified strictness claim. The API has independent restrictive response headers. GitHub Pages cannot enforce this complete header/gateway configuration, so it is not evidence of the proposed controls.

## Controlled rollout and rollback

1. Build/test isolated artifacts; retain checksums and approved content/media. Review the report and all unresolved controls.
2. Deploy only to approved staging with administration disabled. Verify API readiness and current schema, authentication, artifact separation and authorized/unauthorized asset behavior at the gateway.
3. Provision individual staff MFA through an approved audited procedure. Confirm recovery, expiration, role downgrade, refund step-up, exports and audit retention before enabling administration.
4. Rehearse genuine payment sandbox and encrypted restore; obtain legal/content/accessibility review and explicit production approval.
5. Release signed/versioned storefront/API/staff packages independently through the existing cutover gate and runbooks.

Rollback: first set `Administration:Enabled=false` if staff controls fail, revoke affected families and preserve audit evidence. Restore the previous verified frontend artifact and compatible API package through the existing cutover procedure. The additive cart-variant and customer-request migrations have guards against discarding selected options or request records; prefer a reviewed forward fix. See VARIANT_MIGRATION.md and ACCOUNT_REQUESTS_AND_MEDIA.md. Do not restore a database backup casually over live payment history; use the reviewed recovery runbook and reconciliation procedure.
