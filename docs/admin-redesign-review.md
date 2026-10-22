# Amaara Admin UI review

This upgrade changes the admin presentation and interactions. Existing API modules, route paths, permissions, authentication/session operations and business workflows are preserved. Prior payment simulator changes remain intact. No backend code, production configuration or business records were changed for this redesign. There are no new dependencies: the existing Feather icons and native accessible dialog are reused.

## What changed

- Navy/gold tokens, consistent typography and spacing, accessible status colours, and two elevation tokens replace the overlapping brown theme and dashboard styles. All admin-owned colours live in `adminTokens.css`; no admin JSX inline styles remain.
- Navigation is grouped as Overview, Sell, Fulfil, Customers & Money, and Control. Dashboard is first for roles already allowed to see it. The sidebar collapses to a 72px rail, uses a drawer below 768px, scrolls independently and pins the staff card/logout footer.
- Breadcrumbs replace the old home arrow; detail pages retain a list-back link. The keyboard-operable user menu owns the existing reauthentication handler. The floating button and its obsolete CSS are removed.
- Shared primitives provide headers, cards, fields, controls, table states, pagination, status chips, errors, skeletons, notifications, confirmations, row actions, copyable IDs, tooltips, disclosures, tabs, timelines and filters. Tables scroll within their cards and retain sticky headers. Native dialogs retain focus trapping, Escape and focus restoration.
- All existing list/editor tables use the shared table component. Order, customer and production detail pages have main-content and summary/history columns. Existing filters, pagination, forms, validation, private downloads, refunds, production transitions, CSV export, recovery and staff review flows remain available.
- Financial displays use explicit ISO currency labels and two decimals, for example `LKR 1,234.50`. Existing CHF amounts remain CHF; nothing converts money. Display dates use dd/mm/yyyy and Asia/Colombo. Report filter boundaries remain UTC and exclusive as before.

## Seven priority pages: before and after

| Page | Before | After |
| --- | --- | --- |
| Dashboard | Cramped horizontal metrics and split comparison text; inline styles and oversized identifiers. | Four spacious stat cards, separate comparison lines, current Colombo date and personalised welcome, compact recent-order IDs and real top-product sales. Pending-order attention appears only when the API provides a positive count. |
| Payments | Bare filter row, raw states and long references. | Consistent filter card, icon/text chips, copyable gateway/reference IDs, honest empty state with reset, and tidy pagination. Protected refund/reconciliation workflows retain their backend safeguards and review dialogs. |
| Promotions | Unstructured create button and raw active state. | One gold create action, monospace codes, status chips, real usage-versus-limit progress when a limit exists, and a keyboard-operable row action disclosure. |
| Reports | Full-width-looking form controls and adjacent tables without clear separation. | Reporting-period card, UTC helper text, adjacent Apply/CSV controls, financial-separation callout and separate table cards. All six existing report sections are retained. |
| Audit log | Awkward full UUID wrapping, browser-local dates and plain JSON disclosure. | Colombo timestamps with original timestamp tooltips, readable actions with raw-code tooltips, copyable shortened IDs, styled safe-metadata disclosure and resettable filters. |
| Staff | Misaligned role checkboxes and compact review text. | Two-column described role choices, associated field hints, empty-selection danger warning, session-revocation callout and before/after review. Existing authenticator recovery and second-owner protections remain unchanged. |
| Privacy requests | Plain errors and minimally styled request articles. | Honest request queue, elapsed age, state chips, designed loading/error/empty states, retry, and a focused reply form. The retention note stays visible; closing a request still does not erase records. |

## Deliberate limits

The request DTO does not provide requester identity or a formal SLA. The queue therefore shows supplied request details and elapsed age; it does not invent a requester, deadline or overdue state. An SLA policy and any additional identity fields need a separate business/API decision.

There is no global admin search or staff-profile route, so those controls are omitted. No trend percentage is invented. Backend rules that disable financial controls while authoritative state is uncertain are preserved. Historical currencies remain separate even though the main business currency is LKR.

## Reproduce the checks

Use Node 22+ and .NET 8. From PowerShell:

```powershell
cd C:\Nexaura\client
npm.cmd run lint
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
npm.cmd run build:admin
npm.cmd run check:isolation
```

Browser tests use disposable synthetic acceptance records and reserve ports 5178, 5179 and 5198. Stop frontend/test servers occupying those ports before running; the business API can remain running. The dedicated config builds its test host into `.runtime/admin-ui-acceptance-build`, avoiding the business API's running binaries:

```powershell
cd C:\Nexaura\client
npm.cmd exec -- playwright test --config playwright.admin-ui.config.ts e2e/admin e2e/account/privacy-requests.spec.ts
```

For production frontend checks, the same config excludes four development-only harnesses that intentionally import source modules. They remain in the complete development run:

```powershell
$env:AMAARA_E2E_PRODUCTION = '1'
npm.cmd exec -- playwright test --config playwright.admin-ui.config.ts e2e/admin e2e/account/privacy-requests.spec.ts
Remove-Item Env:AMAARA_E2E_PRODUCTION
```

Review the seven pages, collapse/expand the sidebar, open/close the drawer with the keyboard, open the user menu, inspect audit metadata, copy an ID and review/cancel a staff change. New layout checks visit all 21 navigation destinations and three detail pages at 1920/1440/1024/768/390px; the seven priority pages are scanned at every width and every destination is additionally scanned at desktop/mobile widths. Native confirmation workflows and source-module race tests are retained in the regression suite.

See [the initial inventory](admin-redesign-inventory.md), [observed verification results](admin-redesign-verification.md) and [the complete changed-file list](admin-redesign-files.md).
