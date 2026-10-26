# Zahls webhook security

The actual callback is `POST /api/v1/payments/zahls/webhook`, implemented in
`be/Api/Controllers/PaymentsController.cs`. It is anonymous only because Zahls
cannot present a customer token; provider HMAC authentication is mandatory.

`ZahlsWebhookVerifier` follows the
[official signature specification](https://www.zahls.ch/en/developers/guides/webhook):
UTF-8 signing-key bytes, original request-body bytes, HMAC-SHA256 and lowercase
hex `X-Webhook-Signature`. It validates exactly 64 lowercase hexadecimal
characters and compares decoded bytes with `CryptographicOperations.FixedTimeEquals`.
The key is not Base64-decoded. Development never bypasses verification.

The controller accepts JSON, reads at most 64 KiB and never reserializes the
signed body. Missing/invalid signatures return 401. Malformed signed transaction
payloads are rejected by the parser. A valid signature alone cannot identify an
order: reconciliation matches provider, stored reference, merchant identity,
currency, exact minor amount, gateway/transaction identity and TEST invoice
evidence. Unknown or mismatched payments cannot confirm an arbitrary order.

`PaymentWebhookService` hashes the exact body for deduplication and dispatches
payment events to reconciliation and credits to refund processing. Existing SQL
transactions, event identities, state-transition checks, refund balance checks
and inventory reservation rules preserve idempotency. Repeated confirmation
must not consume stock or finalize an order twice. A late confirmation after
inventory release can require payment review; it is not treated as a new sale.
Browser query parameters and return URLs never constitute payment proof.

The webhook-only Node proxy binds `127.0.0.1:5199`. It exposes exactly the POST
path, rejects query/path variants and all other routes/methods, requires JSON and
a correctly shaped signature, enforces the same body limit and preserves raw
bytes/signature while forwarding to `127.0.0.1:5192`. It has a ten-second request
body deadline and fifteen-second upstream deadline. Only the backend
authenticates the HMAC. A syntactically valid fake signature still receives 401.
No public forwarding exists for Swagger, sign-in, customer/admin APIs or fixture
utilities. Historical Datatrans ingress remains an explicit testable option;
the normal CLI selects Zahls.

Zahls requires a response within twenty seconds and supports delivery retries.
Processing is synchronous: SQL commit completes before 204. Failed commits
produce an error and can be retried; there is no volatile queue acknowledgment
that could silently lose an event. The application also has authoritative API
reconciliation for delayed notifications. Provider delivery retries must be
enabled, monitored and tested with the actual merchant. A durable inbox worker
would be a separate scaling improvement, not something this change pretends to
have implemented. Avoid slow work in this request path and monitor latency.

Internal availability logs contain provider, currency, safe configuration code
and reason. Provider keys, signatures, raw payloads and provider error bodies are
not emitted by the new diagnostics/readiness script. Restrict operational logs
to developers/operators. Never log card numbers or CVC; the app uses hosted
checkout and has no custom card collection form.

Verification includes raw Unicode/whitespace preservation, route restrictions,
missing/invalid/altered signatures, oversized requests and SQL duplicate-event
tests. A real temporary public tunnel also returned 404 for Swagger and 401 for
missing/invalid signatures. Genuine Zahls signed delivery and retries remain
blocked on merchant method readiness and dashboard webhook registration.
