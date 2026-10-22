# Admin redesign verification

Verified locally on 23 October 2026. These checks cover the presentation redesign; earlier payment-simulator work in the workspace was preserved.

## Frontend checks

| Check | Observed result |
| --- | --- |
| ESLint | Passed. |
| TypeScript | Passed. |
| Vitest | 220 tests passed across 29 files, including 14 admin component/format cases and 13 token contrast cases. |
| Storefront and admin builds | Passed. |
| Storefront isolation | Passed: admin code remains outside the storefront build. |
| Release artifact privacy | Passed for the frontend outputs; simulator markers and private development files are excluded. |
| Static inspection | No admin JSX inline styles, raw hex colours outside the token file, hardcoded localhost URLs, or browser alert/confirm calls. |
| Contract inspection | Production backend, admin API modules, route registry and permission definitions have no redesign changes. |

The two builds and artifact checks are also rerun after the last interaction fixes. Build/test logs and screenshots are under the ignored `.runtime` directory.

## Browser checks

The complete development selection contains 46 cases. The full run passed 42; focused reruns passed the four corrected cases. The corrections were a keyboard tooltip focus style, duplicate success-message wording, artwork-link target size, and an assertion expecting the old heading structure. All 46 cases have therefore passed across the complete run and focused reruns; this is not a claim of a single clean 46-case run.

The suite retains authentication, role restrictions, navigation without session reloads, form validation, inventory, payment/refund/reconciliation, proof/production/shipping, privacy replies, staff review and original mutation-payload checks. Source-module race-condition harnesses run in development. The dedicated config excludes those four harnesses when testing built production assets.

Layout coverage visits all 21 navigation destinations and three detail routes at 1920, 1440, 1024, 768 and 390px: 120 destination visits. The seven priority pages receive automated WCAG scans at all five widths, and all 24 destinations are scanned at 1440 and 390px. These checks include WCAG 2.2 AA rules, page overflow, keyboard skip navigation, active links, sidebar widths, mobile drawer Escape/focus restoration, user-menu navigation, and runtime errors. The final production-asset run additionally rejects browser console warnings and errors.

Focused cases check safe audit metadata, ID copying, full-ID tooltips, error retry, empty-payment filter reset, native confirmation focus restoration, staff before/after review, and the unchanged staff PUT payload.

The final production-asset selection passed all nine cases in one run (3.5 minutes): all five viewport sweeps, audit interactions, catalog/editor accessibility, privacy/error/empty states and staff review/payload. The five sweeps reported no browser console warnings, console errors or unhandled runtime errors. Its preserved log is `.runtime/admin-redesign-production-final.log`; development logs are `.runtime/admin-redesign-full-development.log`, `.runtime/admin-redesign-targeted-development.log` and `.runtime/admin-redesign-catalog-development.log`.

Screenshots: `.runtime/admin-before-{page}.png` and `.runtime/admin-after-{page}-{width}.png`, where pages are dashboard, payments, promotions, reports, audit, staff and privacy-requests. These contain synthetic acceptance records. Final screenshots were visually inspected for desktop hierarchy, mobile layout, readable audit IDs and the staff-role form.

Automated accessibility scans do not replace manual assistive-technology testing. No claim is made about a full screen-reader audit or every operating-system/browser combination.

## Data and safety

Browser tests use the existing isolated acceptance host and synthetic records. They do not read real browser profiles, use staff credentials, connect to the business SQL database, or issue financial transactions. The business API and storefront remain running; the known local management Vite process is temporarily replaced for the acceptance run and restored afterward.

Formatting changes only display values. LKR and historical CHF remain distinct; no conversion or stored-value mutation occurs. Dates display in Asia/Colombo; report filter boundaries retain their original UTC semantics. The builder hook exposes its existing load callback for retry, and the existing sign-out handler is passed to the user menu through presentation context. Authentication and request behavior remain intact.

See [the review guide and reproducible commands](admin-redesign-review.md) and [changed files](admin-redesign-files.md).
