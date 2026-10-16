# Backend architecture

The .NET 8 application remains `be/be.csproj` and publishes `be.dll`. There are four source layers with feature ownership inside them.

| Layer | Responsibilities | Allowed project dependencies |
| --- | --- | --- |
| `Api` | Controllers, HTTP middleware, binding, status/headers, cookies, request actor and OpenAPI. | Application/domain; infrastructure only in startup composition. |
| `Application` | Feature contracts, queries, business orchestration and integration abstractions. | Domain, its own abstractions, EF query primitives and existing Identity framework services. |
| `Domain` | Entities, states, money rules and order/shipment/production transition policies. | Other domain modules and standard validation/Identity model types. |
| `Infrastructure` | EF mappings/migrations/evidence, identity crypto/persistence, storage, SMTP, payment adapters and background jobs. | Application/domain; no API types. |

Domain entities retain existing data annotations and the ASP.NET Identity user base. This is a practical monolith with enforced ownership, rather than a framework-free domain rewrite. Application code has no controllers, HTTP contexts/form files or provider implementation references.

## Application interfaces

Feature services are concrete when there is no varying integration. Interfaces are reserved for payment providers/webhooks, storage, email, identity token/session services, audit, the request actor and persistence. The pure custom pricing implementation has no redundant interface.

`IApplicationDbContext` exposes EF sets, tracking, saving and the commerce transaction seam. Application code keeps typed EF queries, projections and transaction orchestration. `ApplicationDbContext` implements the seam in Infrastructure. Both resolve to the same scoped instance. No generic repository or parallel unit-of-work wrapper is introduced; [ADR 0002](../adr/0002-ef-persistence-seam.md) explains the trade-off.

`IRequestActor` supplies the principal and explicit audit/session metadata. Its HTTP adapter lives under `Api/Identity`. Storage receives `MediaUpload` streams and metadata; HTTP file adaptation lives under `Api/Files`. No upload API moves private media into public storage.

## HTTP and failures

Controllers retain their original canonical `/api/v1` routes, model-binding parameter names, authorization attributes and request limits. They call feature operations and translate successful data or downloads into HTTP responses. Framework request validation handles malformed inputs; feature validation handles persisted/business relationships.

`CommerceExceptionMiddleware` translates expected business failures into safe ProblemDetails with stable codes, a compatible message and correlation ID. Feature validation uses ValidationProblemDetails with field errors. Unexpected failures reach ASP.NET exception handling and structured logging. Payment ambiguity remains a distinct workflow result rather than a generic success.

Policies are defined once in `Application/Features/Identity/AppPolicies.cs`; role names belong to `Domain/Identity`. Their registries expose read-only collections. The JWT validation hook still reloads database roles on requests, so staff demotion and lockout remain effective before access-token expiry. Frontend permissions remain presentation only.

## Commerce and providers

Checkout creates the order/payment attempt atomically with the existing fingerprint and idempotency key. The SQL-owned serializable commerce lock still coordinates inventory and financial persistence across application instances. Local transactions finish before provider HTTP or SMTP calls. Reservations, consumed/released stock, payment retries, refunds and outbox ordering retain their tested behavior.

`Application/Abstractions/Payments` defines provider-neutral requests/statuses, the gateway and signed-webhook parser. `Infrastructure/Payments/Zahls` owns Zahls options, HTTP payloads, status parsing and signature verification; `Infrastructure/Payments/Datatrans` owns the historical adapter independently. `PaymentWebhookService` applies a verified neutral event through refund/reconciliation workflows. Controllers handle content type and bounded raw bytes and never call provider parsers.

`PaymentGatewayRouter` routes historical reconciliation/refunds by the stored provider. Registering both adapters is intentional: the router needs both and the last single `IPaymentGateway` registration remains the active checkout provider. Tests replace that collection with deterministic fakes. Zahls remains TEST-only and disabled unless its configuration passes availability checks. [ADR 0003](../adr/0003-provider-and-currency-identity.md) records provider/currency constraints.

Money remains `decimal`, with explicit ISO currency and exponent handling. Stored LKR values and CHF values keep their identities; no FX conversion or price relabelling occurs. Order/shipping/custom-design snapshots remain historical facts.

Account statistics expose non-cancelled order totals grouped by currency. The legacy `totalSpent` field is explicitly labelled LKR; customer order history includes currency beside each amount. Order totals are not settlement revenue.

## Persistence and startup

`Infrastructure/Persistence/Configurations` groups 42 existing entity mappings into 14 cohesive modules implementing `IEntityTypeConfiguration<T>`. The context applies them from the assembly. Its evidence partial retains append-only audit/inventory protections. Existing tables, constraints, indexes, delete behavior, concurrency and migration IDs remain unchanged.

The 16 existing migrations and their generated models/snapshot remain under `Infrastructure/Persistence/Migrations`, retaining the `be.Migrations` namespace and historical model strings. Only source imports follow the moved context. A fixture fingerprints all 33 historical payloads; EF's pending-model check independently verifies the current relational model.

`Program.cs` composes Infrastructure, Application, authentication and API setup, builds the app, applies the HTTP pipeline, bootstraps roles/development data, then runs. Named extension modules own those details. Middleware order and development/production bootstrap behavior remain intentional.

`ApplicationDbContextFactory` supports EF design-time operations without starting HTTP hosting or seeding. Offline model checks use the SQL provider without connecting. Online commands require the intended configured connection or explicit `--connection`; development secrets are read only in Development. The repository tool manifest pins EF tooling to the application's EF version.

## Verification and changes

Backend tests live under `tests/be.Tests/Unit`, `Integration`, `Architecture` and `Fixtures`; the isolated browser host remains a separate executable. Real SQL tests opt into clearly named disposable databases. Architecture tests verify source/interface dependencies and compare all 163 original endpoint/security contracts and 15 policy-role assignments. See [testing](testing.md) for commands and verification limits.

Add a feature operation beside its contracts; reuse the current EF and integration seams. Keep authorization and HTTP translation at the endpoint, pure transitions near domain states, and provider-specific logic in its adapter. Add a migration only for an intentional model change and preserve applied migration history.
