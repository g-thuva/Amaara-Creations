# Local payment simulator audit

Starting point: local `fix/admin-auth-dashboard` at `c826980`; working tree initially clean. No production or business-database changes are needed.

- `CheckoutService` already calculates server-authoritative quotes, persists immutable order/payment snapshots, reserves inventory and records idempotent checkout/retry attempts.
- `PaymentReconciliationService`, `PaymentWebhookService` and `RefundService` already verify provider identity, currency, amounts and event replay before applying financial state, inventory and audit effects.
- `FakePaymentGateway` in the test assembly is reusable, but its single `NextStatus` field is insufficient for manually inspecting several independent payments. Extend it with an optional per-transaction store while preserving existing deterministic tests.
- `AcceptanceApiFactory` runs real Identity/JWT and business workflows with captured email and a fake gateway. It supports guarded disposable SQL catalogs; the normal acceptance host uses memory and does not provide an interactive payment page.
- Existing checkout browser tests either stop at a quote or mock transport/payment UI. They do not provide a manual, SQL-backed local hosted-payment simulator.
- Production backend/frontend hosted-payment URL restrictions must remain unchanged. A dev-server-only module alias can map a reserved simulator URL to a loopback hosted-payment page; production builds must reject simulator mode and exclude its files.
- The Zahls adapter remains TEST-only, and Datatrans remains available for historical provider-qualified records. Neither is enabled for simulation. No existing CHF/LKR data is repriced or converted.

Implementation: a separate, non-publishable development executable references existing test fixtures, creates a new uniquely named loopback SQL catalog, seeds synthetic accounts/catalog/shipping, and proxies the normal API. Its local control page signs in synthetic accounts through normal Identity, submits signed fake events through the existing webhook workflow, and exposes SQL-backed inspection/refund/reconciliation controls. Runtime environment, loopback host/origin, run-cookie, catalog name and fresh-database guards fail closed. No real merchant/card/customer data is used. Verify real SQL lifecycle/replay/currency/refund cases, browser journeys and production artifact exclusion; publish a beginner PowerShell/manual guide with observed results.
