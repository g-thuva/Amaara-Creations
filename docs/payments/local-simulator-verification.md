# Local simulator verification

Verified on Windows with local SQL Server, .NET 8, Node 22 and Chrome against the current local implementation. All data used below was synthetic. No provider credentials, external payment requests, business-database migrations, deployment or production configuration changes were used.

| Check | Observed result |
| --- | --- |
| Backend regression suite with real disposable SQL tests enabled | **378 passed, 0 skipped, 0 failed** |
| Frontend unit suite | **193 passed** |
| ESLint / TypeScript | Passed |
| New LKR SQL-backed browser journeys | **4 passed** |
| New CHF SQL-backed browser journeys | **4 passed** |
| Existing checkout, commerce and administrative refund browser regressions | **19 passed** |
| Release tooling tests | **16 passed** |
| Normal API publish, storefront and admin production builds | Succeeded; simulator artifact checks passed |
| Production simulator startup | Refused by server environment guard before SQL access |
| Simulator-mode production build | Refused by Vite build guard |
| Simulator project publish | Produced no publish artifacts |
| Direct SQL inspection script | Read the marked disposable catalog and displayed actual orders, payments, attempts, reservations, refunds, events and stock |

The two-currency SQL journey posts an intentionally false client total and confirms the server persists 227 (two fictional products at 101 plus 25 delivery). Repeated checkout/retry keys return the same attempt. Decline/cancellation release inventory, retry creates a new payment for the same order, a deferred success stays Pending until reconciliation, and duplicate signed events do not apply stock or payment effects again. Conflicting event identifiers and excessive refunds are rejected. Partial, declined and full remaining-balance refunds preserve the original currency and do not restock physical inventory.

The browser journeys open the real dashboard with the synthetic administrator, complete a real storefront checkout into the local hosted simulator, apply verified events on the server, inspect SQL-backed state, return to the storefront, retry decline/cancellation, reconcile a missing webhook, replay events/refunds, and complete a pending refund. Accessibility and 360/768/1440px overflow checks pass on the lab. Boundary cases reject missing run cookies, hostile Origins, DNS rebinding Host values and credential collection through the bridged API.

The existing 19 browser regressions also verify full and partial refunds, provider rejection, ambiguous Pending refunds, safe HTTP errors, stale-response races, refresh recovery, checkout totals and retry behavior. Their acceptance database remains separate from the SQL simulator catalogs.

Retained manual/browser test catalogs start with `AmaaraPhase6Tests_LocalPayments_`; each run has a unique name and database safety marker. Automated backend tests delete only their own fresh, name-checked simulator catalogs. Manual/browser runs retain records. The current manually available catalog is printed by the starter and recorded privately in `.runtime/local-payments/session.json`.

This proves application behavior with a fake provider. Actual Zahls/Datatrans authentication, hosted checkout, provider-issued events, merchant settlement and actual credits remain untested and require genuine provider access. No live-payment capability was enabled.

See [the Windows startup and manual testing guide](local-simulator.md) and [the repository audit](local-simulator-audit.md).
