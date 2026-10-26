# Zahls TEST integration audit

Audited on 27 October 2026 in `C:\Nexaura`, branch `develop`.

## Findings and verified cause

The previous `PAYMENT_METHOD_UNVERIFIED` was an application validation error:
`ZahlsPaymentGateway.ValidateConfiguration` required a nonempty `PaymentMethods`
array, and `InitializeAsync` always serialized that array as `pm`.
Zahls describes `pm` among optional Gateway inputs as a restriction on enabled
methods. Empty application configuration now **omits `pm`**; malformed, null or
duplicate filters still fail. Provider readiness remains independent.
[Official Gateway guide](https://www.zahls.ch/en/developers/guides/gateway/build).

Read-only calls against the actual merchant API returned HTTP 200 with successful
envelopes for `SignatureCheck/` and `PaymentProvider/` under `v1.16`, using the
existing private key. The provider list contains PayPal, with **zero
`paymentMethods` and zero `activePaymentMethods`**. This proves authenticated API
access and a provider listing. It does not prove a usable TEST payment method,
merchant eligibility, sandbox mode or CHF support. The original webhook URL was
also empty/invalid. No real Gateway or refund was created during this audit.

The prior configuration asserted both readiness flags without accompanying
evidence. They were reset to false. Original merchant settings and differing
source values were preserved privately; no existing secrets were discarded.

## Architecture

| Area | Implementation and behavior |
|---|---|
| Provider selection | `Infrastructure/DependencyInjection.cs` registers Datatrans first and Zahls last. Single `IPaymentGateway` resolution selects Zahls for new checkout and retries. `PaymentGatewayRouter` resolves the provider recorded on historical payments. No fallback selects another provider. |
| Authentication | Ordinary React entry and `authApi` use Identity email/password, refresh and logout. Staff authentication retains MFA and permission verification. Simulator Vite aliases and synthetic sign-in entry points were removed. |
| Pricing | `CheckoutPricingService` loads the customer's cart/address, validates product/design/shipping currencies and calculates discounts, taxes, shipping and fees on the server. No implicit CHF/LKR conversion. |
| Availability | `EnsureAvailable` checks enablement, TEST-only mode, CHF, independently verified provider/currency flags, credentials, approved API settings, optional filter syntax, public callback and frontend return base. Quotes return unavailable without merchant details; safe configuration reasons go to internal logs. |
| Attempts | `CheckoutService` persists immutable order totals, payment attempts, ownership, request fingerprints and inventory reservations before provider initialization. Duplicate keys reuse attempts. Ambiguous network failures remain recoverable rather than blindly repeating a POST. |
| Gateway | Real `HttpClient` calls to `https://api.zahls.ch/v1.16/Gateway/?instance=…`, `X-API-KEY`, JSON. The existing official SDK confirms header authentication and array `data` envelopes. No fabricated success path exists. |
| Payload | Positive exact CHF minor units, unique stored payment reference, order purpose, email/optional phone, backend redirect URLs. Optional basket includes products and shipping only when its sum exactly matches the charged total; discounts/tax/fees are already included in the authoritative amount. An inconsistent basket is omitted. Optional `pm` is sent only when nonempty. |
| Hosted link | Backend requires HTTPS/443, no user info or fragment, and either `pay.zahls.ch` or the configured merchant instance host. The stored response URL is returned to React; React applies its existing payment-host/path policy and never constructs a gateway URL. Actual merchant Gateway-link shape still needs external validation. |
| HTTP behavior | Redirect following disabled, bounded timeout, generic safe errors, strict response identifiers/envelope/optional returned amount/currency/reference checks. Both declared and chunked bodies are bounded to 1 MiB. No automatic initiation/refund POST retry. |
| Webhook | Exact JSON callback route; 64 KiB raw body limit; HMAC authentication before parsing; provider-qualified reconciliation/refund routing. See [security audit](ZAHLS_WEBHOOK_SECURITY.md). |
| Reconciliation | Authoritative transaction lookup checks merchant, reference, currency, amount and TEST/invoice evidence. A Gateway without a matching transaction never becomes Paid. Browser return URLs cannot confirm payment. |
| SQL | Existing Orders, Payments, CheckoutAttempts, PaymentWebhookEvents, InventoryReservations and Refunds remain authoritative. Transactions/idempotency preserve one inventory consumption and finalization. No schema migration was added. |
| Refunds | Existing staff permissions/MFA and persisted idempotency remain. Zahls validates original transaction, TEST evidence, amount, provider refund balance and refundable flags before calling the real refund endpoint. Ambiguous outcomes remain pending. No synthetic refund success. |
| Historical Datatrans | Configuration disables initiation, while its adapter/parser/router and historical identifiers remain. Historical maintenance needs explicit valid sandbox settings; this task did not enable it or rewrite records. |

SDK sources: [communicator](https://github.com/zahls/zahls-php/blob/main/lib/Zahls/Communicator.php),
[Gateway model](https://github.com/zahls/zahls-php/blob/main/lib/Zahls/Models/Request/Gateway.php).
The SDK lists versions through 1.15; the configured 1.16 authenticated GETs were
verified directly. POST creation, merchant-host URL shape, transaction and refund
responses still require a real eligible TEST provider. Mocked contracts are
explicitly not external validation.

## Runtime removals and preserved tests

Removed `client/dev/*`, the simulator Vite plugin/aliases, simulator browser suite
and npm command, executable `tests/be.PaymentSimulator`, backend simulator server
and embedded UI, and the four simulator operational scripts. Removed the
simulator env flag and restored only its matching local API/media/management
URLs. Unrelated local settings were preserved. Checkout shows **Secure online
payment**, genuine Zahls TEST wording and the existing gated Pay securely action.

Retained the useful SQL lifecycle harness under
`tests/be.Tests/Fixtures/Payments` as `PaymentWorkflowFixture`,
`PaymentFixtureSafety` and `SignedFixtureWebhookParser`. Its fake provider is
named `ContractTestGateway`, is test-assembly-only and has no executable hosted
screen or simulator endpoints. Normal API boundary tests still reject retired
routes. Production/release checks reject obsolete simulator artifacts/settings.
Existing historical disposable databases were retained and are not attached to
the normal application. Automated tests clean up only their own new catalogs.

## Remaining external work and release gate

Activate an eligible payment method inside Zahls, establish genuine TEST/sandbox
mode and CHF acceptance, register the signed JSON webhook, and obtain the
provider's official test payment details. Confirm a real hosted Gateway, signed
notification, SQL order transition, duplicate callback, failure/cancellation and
supported refunds. Only then set the corresponding readiness flags with recorded
evidence. Connecting direct PayPal sandbox credentials is not a substitute for
Zahls exposing an active method.

LIVE is rejected by the current adapter. Production remains gated on separate
approval, secure secrets, approved HTTPS origins, stable webhook ingress,
operational monitoring, retry/reconciliation review, release security checks and
genuine external TEST evidence. Do not set flags just to enable the button.
