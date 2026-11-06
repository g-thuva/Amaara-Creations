# Customer account controls

Implemented locally on 6 November 2026. Admin and SuperAdmin can manage customer access from Customers → Account actions or the customer detail page.

| Action | Result |
| --- | --- |
| Suspend | Ends existing sessions and stops authenticated access until reactivation. |
| Block | Ends existing sessions and blocks authenticated access until explicit reactivation. |
| Remove | Ends access and hides the account from the normal customer list, retaining identity, orders, payments, reviews and audit history. |
| Reactivate | Restores eligibility to sign in; previously revoked sessions remain invalid. |

Removed accounts are available through the Removed filter and the explicit all-status filter. The existing owner-only privacy-review workflow remains available separately. Actions require a 10–500 character reason; removal also requires acknowledgment that historical records are retained.

The server checks the operator's administrator role and recent MFA, rejects staff/mixed-role/self targets, and atomically saves account state, stamp changes, session revocation and audit evidence. Each action supplies the current account version. A conflicting edit returns 409; the dialog can reload the latest status while keeping the operator's draft for an explicit retry. Failed requests preserve the form, and cancellation does not change access. Removing or restoring the last result on a filtered page returns the list to a populated page.

The new Removed value uses the existing string account-status column. EF model verification reports no schema changes since the current account-lifecycle migration.

## Verification

- 60 backend regressions passed with no skips, including SQL persistence across an API restart, concurrent decisions, session invalidation, retained financial history, permission checks, MFA sign-in and compatibility contracts. Results: `.runtime/customer-access-test-results/customer-access.trx`.
- Seven browser workflows passed across the final six-case run and the focused pagination check. They cover desktop/mobile lifecycle actions, removal/restoration, stale edits, failed requests, cancellation/focus, support permissions, the existing owner-review workflow and pagination. The first browser run exposed an exact-label issue and mobile email overflow; both were corrected before the passing rerun.
- Desktop/mobile removal dialogs passed the configured axe accessibility checks. Screenshots were visually reviewed at 1440px and 390px.
- Scoped ESLint, TypeScript, the final staff production build and EF model drift checks passed. Package restore used cached dependencies; the restricted network prevented NuGet vulnerability-data retrieval.
- The verified backend was loaded into the local Development API on port 5192 with Windows SQL authentication. Readiness returns 200 and Swagger exposes the new Removed status; the local admin application returns 200 on port 5173.

Runtime process details are recorded in `.runtime/customer-access-live-api.json`; API output is in `.runtime/customer-access-live-api.native.stdout.log` and `.runtime/customer-access-live-api.native.stderr.log`. Browser checks used isolated acceptance services and synthetic customers; those temporary services were stopped afterward.
