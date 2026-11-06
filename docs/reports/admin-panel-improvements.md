# Admin panel implementation and verification

Date: 6 November 2026. Base: `develop` at `7635ea1` (the earlier requested main/develop merge was already pushed). This report covers the subsequent local admin implementation. The existing React/Vite staff application, .NET API, EF persistence, navy/gold identity and commerce services are retained.

## A. Root causes and resulting changes

- Shipping presented creation and management together in large inline forms. Zones and methods now have searchable management lists, explicit availability actions and shared create/edit dialogs.
- Shared form layouts lacked consistent vertical rhythm and checkbox targets. Fields now have reusable spacing, separated accessible labels/descriptions, larger clickable rows and responsive dialog columns.
- Wide tables constrained empty/error feedback and captions to their scrolling content. Feedback now sits outside the horizontal table viewport; visible captions fit the card. Tables retain horizontal scrolling for columns, while the page itself fits the screen.
- Customers had operational history but no administrative access lifecycle. Status, decisions, revocation, permissions, concurrency and controlled removal review now persist in the backend.
- Reviews defaulted to manual approval. New eligible submissions publish directly, while subsequent moderation remains authoritative and auditable.
- Settings collected validation errors without rejecting writes and exposed arbitrary keys. Validation now runs before mutation, and the editor exposes 16 supported settings with existing storefront consumers.
- Orders lacked date/sort controls, some repeated-submit protection depended on React rendering, and transition audit metadata omitted the previous state. These gaps are corrected without extending the financial state machine.
- Screenshot inspection found broken media thumbnails, inconsistent builder labels, singular content-count grammar and dashboard icons overlapping values. These are corrected. Staff HTML also no longer requests unused external Google Fonts; it uses the existing bundled Inter.
- A checkout regression exposed a 6px storefront overflow at 320px, caused by gallery dots competing with navigation arrows. The dots now wrap; arrows retain their size.

## B. Page-by-page UX/UI work

Every row below was included in live browser navigation/layout checks at 1920, 1440, 1024, 768 and 390px. Desktop (1440) and mobile (390) screenshots were inspected. Automated axe checks cover every listed operational route at 1440/390 and the existing key-page set at every width. These checks use the real application with isolated acceptance fixtures, not production customer records. They do not constitute certification or visual inspection of every possible data state.

| Page / route | Finding and implemented change | Functional verification |
| --- | --- | --- |
| Dashboard `/admin/dashboard` | Preserved genuine totals and explicit unavailable history; reserved heading height prevents KPI icons overlapping values. | Dashboard APIs, authentication restoration, five-width bounds/keyboard/axe; value/icon separation checked. |
| Products `/admin/products` | Shared field/dialog/checkbox spacing; bounded accessible fallback for unavailable thumbnails. | Real create/edit, variants, media ordering, archive, validation, duplicate-save protection and failed-save draft retention. |
| Categories `/admin/categories` | Shared form rhythm and dialog behavior; preserved hierarchy. | Create/edit, hierarchy and validation journeys. |
| Collections `/admin/collections` | Empty feedback no longer clips inside a wide mobile table; shared dialogs. | Creation/editing and member retention across API pages. |
| Media `/admin/media` | Unavailable images have a stable 48px labelled fallback; shared table behavior. | Upload, ordering, deletion/selection workflows and forced-404 fallback check. |
| Content `/admin/content` | Shared form/dialog spacing and correct singular/plural page count. | Real page/section edits and saved-state refresh. |
| Promotions `/admin/promotions` | Shared inputs, confirmations and unclipped mobile empty-state action. | Disabled offer creation/editing, redemption history, preserved rules. |
| Custom Builder `/admin/custom-builder` | Human-readable numeric labels; consistent checkbox/form spacing; retained dedicated draft editor and action bar. | Configuration navigation, existing editor workflows and role checks. |
| Orders `/admin/orders` | Search/state filters extended with UTC date range and sorting; bounded server pagination. | Inclusive date boundaries, sort/search/page tests, real order navigation. |
| Custom Designs `/admin/custom-designs` | Preserved filtered production queue; shared spacing/table feedback. | Proof approval, production prerequisites and fulfilment journey. |
| Quote Requests `/admin/quote-requests` | Mobile empty feedback fits the card; retained separation between replies and orders/payments. | Existing scoped request/reply API tests; layout, keyboard and axe. |
| Inventory `/admin/inventory` | Shared forms/confirmations and movement empty-state presentation; retained independent pagination. | Confirmed adjustment records a ledger entry; product changes reset movement pagination. |
| Shipping `/admin/shipping` | Replaced always-open forms with zone/method lists, search/filter, counts, edit and confirmed availability actions. Grouped dialog fields and conditional pickup controls; preserved all existing fields. | Real save/checkout eligibility journey, dependent range errors, draft protection, keyboard/axe and 1440/768/390 dialog target/spacing checks. |
| Customers `/admin/customers` | Real status/order counts, name/email search, status filter, sorting and pagination. | Backend filtering and real customer lifecycle browser journey. |
| Payments `/admin/payments` | Preserved financial controls; shared spacing, filters and unclipped empty/retry feedback. | Partial/full refunds, sequential refunds, provider rejection, ambiguous pending outcomes, stale refresh and permission regressions. |
| Reviews `/admin/reviews` | Replaced row-embedded forms with reasoned action dialogs; search/status/rating/product filters, customer/product links and paginated review history. | Direct publish, hide, restore, remove, rating/distribution and audit journey. |
| Privacy Requests `/admin/privacy-requests` | Preserved owner-only retention queue; empty/error feedback fits mobile cards. Customer removal now creates an actual review request. | Removal creation, record retention, permission tests and retry journey. |
| Reports `/admin/reports` | Preserved currency/environment separation and export semantics; card captions and empty feedback fit mobile. | Analyst export, reporting regressions and responsive audit. |
| Audit Log `/admin/audit` | Preserved read-only evidence, filters, safe metadata and copy controls; shared spacing. | Keyboard copy/disclosure/tooltip journey and access checks. |
| Staff `/admin/staff` | Shared labelled checkbox targets, form spacing and confirmations; preserved owner recovery controls. | Role review/focus restoration, MFA recovery and privileged-scope regressions. |
| Settings `/admin/settings` | Supported-setting catalogue, groups/search, real editor dialogs, visibility/default state and consumer descriptions. | Owner-only API validation, persisted changes, actual storefront footer consumption. |
| Order detail `/admin/orders/:id` | Preserved breakdown/history/payment/refund/shipment controls; synchronous mutation locks and destructive cancellation confirmation. | Production/shipping/refund journeys, malformed historical snapshots, refresh and race checks. |
| Customer detail `/admin/customers/:id` | Status/reason, controlled access dialogs, real account-decision history/removal requests; retained addresses/orders/reviews and currency-specific financial totals. | Revocation/reactivation/removal, stale-version denial and retained-record checks. |
| Production detail `/admin/custom-designs/:id` | Shared form/table behavior; preserved proof and production controls. | Proof-to-production-to-shipment journey and malformed snapshot recovery. |

Shared login, enrollment and MFA states were exercised by the authentication browser suites. Dense tables intentionally scroll horizontally within their own named keyboard-accessible regions. Large catalogues use page scrolling rather than a second constrained vertical table viewport.

## C. Customer management

The persisted states are `Active`, `Suspended`, `Blocked` and `PendingDeletion`. Existing accounts migrate to Active; Identity's temporary failed-login lockout remains separate.

Admin and SuperAdmin may suspend, block or reactivate a pure Customer account. CustomerSupport may view customers but cannot change access. Only SuperAdmin may enter or leave PendingDeletion. Staff/mixed-role targets and self-targeting are rejected. Sensitive access actions require an active management session with MFA less than five minutes old.

Every change requires a 10–500 character reason, confirmation and the current Identity concurrency stamp. A stale decision returns 409. Status, security/concurrency stamp rotation, refresh-session revocation, audit and any removal request share one transaction. SQL tests exercise competing decisions and a fresh API instance reading the persisted result.

JWT validation, login, refresh, reauthentication and MFA completion reject non-active accounts. Reactivation permits a new normal login; old access tokens and revoked session families remain invalid. No expiry-based automatic reactivation was added because no suspension-duration policy exists.

Removal requires a separate acknowledgment and opens the existing privacy-request queue. It disables access and preserves orders, payments, reviews, reconciliation and audit records. It does not perform deletion or anonymization. Internal decision reasons stay in restricted customer history/audit; customer-visible request details contain only a neutral review explanation. Closing a privacy request does not erase records or automatically reactivate the account.

Customer lists are paginated. Detail histories are bounded and labelled: 50 orders, designs and reviews, 30 recent account decisions and 10 removal requests. Financial totals retain their currency and provider-environment grouping; no synthetic last-activity or spend metric was introduced.

## D. Review management

New validated customer reviews and normal owner edits publish directly. Existing customer ownership, input validation and duplicate rules remain enforced. The verified-purchase badge retains its existing fulfilled/paid-order rule; it is not an invented verification signal.

Authorized review staff can hide, restore and remove using explicit reasons and current row versions. Hidden reviews remain hidden when edited. Moderator removal creates a `Removed` tombstone that the owner cannot edit, delete or recreate to bypass moderation. Owner deletion remains distinct; deleting a hidden review does not clear the moderation restriction. Legacy Pending rows are retained for explicit review, rather than bulk-published during migration.

Public queries calculate count, average and rating distribution from Published rows only. Moderation refreshes staff data and public queries return the authoritative result; an already-open storefront is updated on its normal data refresh, not by a new realtime push service. Publication, edits and moderation append audit evidence. The history endpoint is review-scoped, paginated and protected by ManageReviews. Its dialog includes the full review, rating, product/customer links where permitted, verification, submission time, current state and last reason above the audit decisions.

The admin list API and field mapping were verified with a genuinely submitted fixture review appearing in both public and staff queries. The original screenshot's empty list was not reproduced as an authentication, mapping or pagination defect. Its production data contents were not inspected or populated merely to replace the empty state.

## E. Orders and financial safeguards

Staff can inspect orders and use server-permitted transitions, existing shipment/tracking controls, supported cancellations and existing payment/refund workflows. The list now supports bounded search/state/date/sort queries. UTC end dates include the selected day; Reports retains its separately labelled exclusive-before semantics.

Cancellation remains rejected while an unresolved Pending, Authorized, Paid or PartiallyRefunded payment requires payment handling. Inventory release remains part of the existing cancellation transaction. Shipping requires a finalized eligible order, valid shipment progression and completed custom proof/production prerequisites. Tracking links retain HTTPS validation. Transition audits now include the previous state and reason.

Order and shipment submissions acquire synchronous locks before awaiting the request. Refund idempotency, pending-amount reservations, provider result handling, stock consistency and server state-machine enforcement remain in the existing services. CHF/LKR rules, production-rate exclusion and Zahls integration were not changed. Test financial journeys use the repository's isolated acceptance gateway; no live charge or refund was made.

## F. Persisted dynamic configuration

SuperAdmin can edit the following catalogue through Store settings. Values, visibility and audit evidence persist together; current editors send the last-updated value for stale-write rejection. Invalid email/length/unsafe URL/unknown or secret-like keys are rejected before writing. Links accept HTTPS, or a single-slash storefront path where the consumer supports one; credentials, protocol-relative paths and script URLs are rejected. Known values are text, not executable markup.

| Setting key | Actual consumer |
| --- | --- |
| `site.name` | Navigation/home-link identity and footer |
| `contact.email` | Contact page and footer email link |
| `contact.phone` | Contact page and footer phone link |
| `contact.address` | Contact page and footer address |
| `contact.hours` | Contact page business hours |
| `contact.whatsapp` | Contact page messaging link |
| `social.instagram` | Contact page and footer |
| `social.tiktok` | Contact page and footer |
| `social.facebook` | Contact page |
| `announcement.text` | Announcement above storefront navigation |
| `announcement.url` | Announcement destination |
| `delivery.note` | Home trust information and product delivery note |
| `business.notice` | Home trust information |
| `custom.notice` | Home custom-order trust information |
| `policy.privacy.url` | Footer privacy link |
| `policy.terms.url` | Footer terms link |

Storefront content refreshes when the page regains focus/visibility. The browser journey confirms an owner-saved contact email survives reload and appears in the actual storefront footer. Public visibility determines API exposure; existing fallback copy remains available when a public value is absent. Existing uncatalogued rows are retained and counted, without presenting unconnected controls as supported configuration.

Shipping, promotions, catalogue and CMS continue using their dedicated persistent management pages. Infrastructure, provider secrets, authentication, currency policy and retention policy remain deployment/business configuration.

## G. Important files

- Shared UI: `client/src/features/admin/shared/components/{AdminUi,AdminFormDialog,DataTable,AdminThumbnail,AuditDecision}.jsx`, `shared/styles/adminBase.css`, dashboard `Dashboard.css`.
- Shipping: `client/src/features/admin/features/shipping/{ShippingPage,ShippingZoneForm,ShippingMethodForm,OrderShipments}.jsx`, `useShippingConfiguration.js`, `shippingForm.js`; backend `ShippingManagementService.cs`.
- Customers: `CustomerAccountActions.jsx`, `CustomerActivity.jsx`, list/detail/summary/hooks/API modules; backend `CustomerAccountService.cs`, customer query/contracts, `CustomerAccountStatus.cs` and `User.cs`.
- Authentication/authorization: `AppPolicies.cs`, `AuthenticationExtensions.cs`, identity/reauthentication/refresh/second-factor services, frontend `permissions.js`.
- Reviews: review list/action/history components and hooks/API, storefront review feedback; backend `ReviewService.cs`, `ReviewsController.cs`, review entity/configuration.
- Orders: list/filter/contracts, transition/shipment submission controls; backend `OrderService.cs`, `OrdersController.cs`, `OrderLifecycleService.cs`.
- Settings: setting list/editor/catalogue hooks/API, `SiteContentProvider.jsx`; backend `SettingsService.cs`, `StoreSettingCatalog.cs`, requests/controller.
- Migration: `be/Infrastructure/Persistence/Migrations/20261106064053_AdminCustomerLifecycleAndDirectReviews.cs`, its designer and current model snapshot. Adds three User columns/index and changes only the default for new Review records. Down refuses to discard existing customer-access decisions.
- Regression fixes: media/product thumbnail tables, builder pricing labels, content page count, `client/vite.config.js`, storefront gallery CSS; existing unused callback/index lint defects.
- Tests: `CustomerLifecycleTests`, `ReviewModerationTests`, `StoreConfigurationTests`, `AdminOrderFiltersTests`, legacy SQL identity fixture and scoped payment fixtures; `client/e2e/admin/account-review-settings.spec.ts`, responsive/operations/shipping/dialog/architecture/payment tests and `AuditDecision.test.jsx`.
- Architecture/security: `docs/architecture/admin.md`, `docs/security/ROLE_PERMISSION_MATRIX.md`.

Historical migration/designer fingerprints remain checked. Only the mutable current EF model snapshot is excluded from the historical byte fingerprint; EF model-drift verification covers that snapshot. SQL fixtures pinned to older migrations explicitly insert their historical identity columns. Payment test fixtures now select their intended QA product/currency rather than whichever seed row sorts first.

## H. Verification results

Backend and frontend checks used .NET 8 and Node 22.23.4. The SQL run uses Windows authentication and uniquely named `AmaaraPhase6Tests_*` databases. Test-created SQL databases are disposed by the tests; the application database was not reset or migrated.

| Check | Actual result / evidence |
| --- | --- |
| Complete backend suite, including SQL | **487 passed, 0 failed, 0 skipped**, 58s. `.runtime/admin-panel-results/admin-panel-full-final.trx`. Includes the final customer-visible privacy-details assertion. |
| Complete frontend unit/component suite | **296 passed, 0 failed**. `.runtime/admin-panel-vitest-final.json`. |
| Full admin browser run | **80 passed, 4 failed out of 84**. Two cold login-page timeouts and two dashboard overlay bounds failures. `.runtime/admin-panel-browser-final-complete`. These failures were investigated, not excluded from the count. |
| Focused authentication/navigation/responsive/dashboard rerun | **18 passed, 1 failed out of 19**. Both login checks passed; all five widths passed with the new KPI/caption/feedback bounds assertions. One narrower desktop dashboard overlay still exceeded its chart area. `.runtime/admin-panel-recheck-final`. |
| Final dashboard regressions after layout correction | **5 passed, 0 failed**, covering real/empty/error/loading/chart states at six widths, plus the 1280×800 height check. `.runtime/admin-panel-dashboard-final`. |
| Production artifact browser verification | **14 passed, 0 failed**, 1.2m, after correcting the test-only HTTPS destination. Includes all new management journeys, shared-login/MFA separation and real shipping-to-checkout integration. `.runtime/admin-panel-production-verified`. |
| Checkout/cart regressions | **11 distinct checks verified across the combined run and cart rerun**. The 320px overflow was fixed, then both real cart journeys passed in the clean 9-test responsive/cart rerun (`.runtime/admin-panel-responsive-final`). This is not a claim of one clean 11-test run. |
| Lint / TypeScript | Passed, no lint warnings. |
| Vite storefront and staff builds | Passed; staff HTML has no external Google Fonts request. |
| Storefront bundle isolation | Passed: 40 manifest entries, 47 assets; administrative modules remain outside the storefront bundle. |
| EF model drift | Passed: no model changes since the last migration. Additive upgrade SQL generated/reviewed at `.runtime/admin-panel-upgrade.sql`. |
| Secret scan / whitespace | Passed: 1,059 text files, 0 secret candidates; `git diff --check` passed. |

All **84 distinct admin checks** have passing verification across the full and corrective focused runs. This is not a claim of a single clean 84-test execution. All 21 navigation destinations and the three operational detail routes were checked at 1920, 1440, 1024, 768 and 390px. Automated axe found no violations for the configured WCAG 2/2.1/2.2 A/AA rules on the audited routes. Zone/method dialogs were checked at 1440/768/390 for spacing, ≥44px checkbox rows, available actions, overflow and axe. Review detail/history was checked at 1440/390 with full configured axe rules. Screenshots are in `.runtime/admin-panel-evidence` and were inspected, including corrected dashboard values, mobile feedback/captions and review details.

The first production trial passed the 10 operational checks but failed four shared-login checks. Traces identified an invalid HTTP storefront destination in the test build. Production correctly rejected it. `playwright.config.ts` now supplies the approved HTTPS fixture hostname, and `e2e/fixtures.ts` maps that navigation to the real local storefront artifact. Application URL validation was preserved. The two earlier development login timeouts did not recur in the focused run; their individual cause was not established.

Commands used (PowerShell; frontend commands run from `client`, with the local Node 22 directory added to PATH):

```powershell
$env:AMAARA_TEST_SQL_CONNECTION = 'Server=localhost;Database=AmaaraPhase6Tests_AdminPanelFinal_' + [Guid]::NewGuid().ToString('N') + ';Integrated Security=true;Encrypt=false;TrustServerCertificate=true'
dotnet test tests/be.Tests/be.Tests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false --logger 'trx;LogFileName=admin-panel-full-final.trx' --results-directory .runtime/admin-panel-results
dotnet ef migrations has-pending-model-changes --project be/be.csproj --startup-project be/be.csproj --configuration Release --no-build
python scripts/scan-secrets.py
git diff --check

# From client:
node node_modules/eslint/bin/eslint.js .
node node_modules/typescript/bin/tsc --noEmit
node node_modules/vitest/vitest.mjs run --reporter=json --outputFile=../.runtime/admin-panel-vitest-final.json
node node_modules/vite/bin/vite.js build
node node_modules/vite/bin/vite.js build --mode admin
node scripts/check-storefront-isolation.mjs
node node_modules/@playwright/test/cli.js test --project=staff --output ../.runtime/admin-panel-browser-final-complete
node node_modules/@playwright/test/cli.js test --project=staff e2e/admin/authentication.spec.ts e2e/admin/navigation-loading.spec.ts e2e/admin/sales-command.spec.ts e2e/admin/redesign.spec.ts --trace retain-on-failure --output ../.runtime/admin-panel-recheck-final
node node_modules/@playwright/test/cli.js test --project=staff e2e/admin/sales-command.spec.ts --output ../.runtime/admin-panel-dashboard-final
$env:AMAARA_E2E_PRODUCTION = '1'
node node_modules/@playwright/test/cli.js test --project=staff e2e/admin/authentication.spec.ts e2e/admin/account-review-settings.spec.ts e2e/admin/shipping.spec.ts --trace retain-on-failure --output ../.runtime/admin-panel-production-verified
```

## I. Delivery limits and remaining operational work

- This implementation is local and reviewable on develop. The earlier branch merge was pushed; the subsequent admin changes have not been committed, pushed or deployed.
- The additive migration was generated/reviewed and exercised on disposable SQL databases. It has not been applied to the application/customer database. Deployment must apply this migration with the normal reviewed database release process. The guarded rollback must not be bypassed to discard access restrictions.
- Irreversible removal/anonymization and retention duration require a defined business procedure and explicit authorization. This implementation provides controlled deactivation and review, preserving financial/audit history.
- No live Zahls provider transaction, production-approved shipping-rate change or currency-policy change was attempted. Acceptance gateway and isolated SQL regressions verify application behavior, not live provider credentials, webhooks or merchant settlement.
- Browser checks used installed Chrome on Windows. Safari/Firefox, physical assistive technologies, production traffic/performance and exhaustive security penetration testing were not run.
- Two cold development-login checks timed out in the full browser run and passed the focused rerun. Their precise cause remains unconfirmed; preserve the recorded failure history when assessing CI stability.
- Existing legacy Pending reviews remain pending; any bulk publication would require an explicit decision.
- Review media attachments and a review date-range filter are not supported by the current model/query contract. No fictitious attachment data or unsupported returns/order states were added.
