# Admin Sell form redesign: implementation and verification

Implemented on `develop`, starting from `4013c98`. The existing React/Vite application, feature API modules and native dialog component were reused. No dependency, backend, database, payment, authentication or authorization changes were introduced.

## A. Root cause

`ProductsPage` always rendered `ProductForm` after the catalogue. The New Product button only called `resetForm`; it had no open state or dedicated editing surface. Editing also populated that distant form. Categories, Collections and Content similarly kept forms below their management tables. Promotions revealed an inline form. No legacy `scrollIntoView` handler was responsible for the inspected product interaction.

| Before | After | Purpose |
| --- | --- | --- |
| Reset an always-mounted product form below the list | Conditionally mount one product editor in a native modal | Make the action visible immediately without moving the catalogue |
| Document-length forms and inconsistent actions | Fixed header/footer with one scrolling dialog body | Keep context and actions accessible |
| Each dialog saved/restored body overflow independently | Reference-counted scroll locking and focus restoration without scrolling | Keep nested confirmations safe and clean up on navigation |
| Collection editing initialized product selection to empty | Load every existing membership and retain it during metadata edits | Prevent accidental removal of associations |

## B. Implementation

`AdminFormDialog` provides responsive widths, labelled title/description, persistent actions, pending/error states, backdrop dismissal and an accessible discard confirmation. It builds on the existing native `Dialog`, which supplies top-layer stacking, background inertness, keyboard containment and focus restoration. Scroll locks preserve existing document styles, handle nested teardown in either order and avoid adding a gutter below the page's minimum width.

Product draft state and submissions now live in `ProductEditor` / `useProductForm`, independently of the catalogue. Opening or typing in the editor does not reload the product list. Fields, collections, categories, currency, stock restrictions and row-version payloads are preserved. Existing media loads within editing; media and variants retain their own established operations in a separate wide dialog.

The media picker preserves selected files and previews through rerenders, supports selection/drop/removal/order changes, cleans up object URLs and blocks changes while saving. A successful product save is remembered if a subsequent upload fails: retry uploads to that product instead of issuing another create. Successfully uploaded files are removed from the pending queue. Closing after a partial upload refreshes the already-saved product.

## C. Interaction behavior

- Clean forms close through the backdrop, Escape, Cancel or Close. Dirty forms offer Continue Editing and Discard Changes through the same policy.
- Background content remains visible and inert. The dialog body scrolls; its header and footer remain visible. Catalogue document and table scroll positions are preserved.
- Validation and API errors remain inside the editor with entered values intact. Duplicate submissions are blocked by the existing mutation lock.
- Successful saves close/reset the form, refresh existing resources and show feedback. Product search/status filters remain intact; Find saved product is an explicit action to locate a newly saved item.
- Route changes release both parent and confirmation locks. Full-document navigation/reload also receives native unsaved-change protection.

## D. Sell-section status

| Section | Status |
| --- | --- |
| Products | Updated: create/edit dialogs, independent form state, media previews/uploads, safe upload retries and a media/variants dialog |
| Categories | Updated: compact create/edit dialog; hierarchy, sort, visibility and validation preserved |
| Collections | Updated: compact create/edit dialog; all existing memberships retained, including more than 100 members |
| Media | Read-only library retained; its existing editable product-media workflow now uses the product tools dialog |
| Content | Updated: wide create/edit dialog retaining title, slug, summary, publication and dynamic sections |
| Promotions | Updated: create/edit dialog retaining currency, dates, eligibility, limits and concurrency fields |
| Custom Builder | Dedicated editor retained because pricing, option tables, quote testing and publishing form a larger workflow; actions remain visible and draft discard is confirmed |

## E. Verification

Commands below ran from `client` unless stated otherwise. Browser tests used the existing isolated acceptance host, disposable accounts and its in-memory database. They did not submit real administrator credentials or modify the development/business database. Local Playwright configurations under `.runtime` selected the existing projects and a freshly compiled acceptance host.

| Command/check | Actual result |
| --- | --- |
| `npm.cmd test` | 275 tests passed across 35 files, including four scroll-lock regressions |
| `npm.cmd run typecheck` | Passed |
| ESLint over all changed/new JavaScript and JSX files | Passed |
| `npm.cmd run lint` | Existing storefront error: unused `idx` in `InstagramShowcase.jsx:141`; existing dependency warning in `CustomDropdown.jsx:92` |
| `npm.cmd run build` | Passed |
| `npm.cmd run build:admin` | Passed, including the final label/helper-text polish |
| `npm.cmd run check:isolation` | Passed: 40 storefront manifest entries and 47 assets |
| `npx.cmd playwright test --config ../.runtime/admin-form.playwright.config.ts --workers 1` | Final form suite: 20 passed |
| Focused required/server validation after final copy polish | One passed |
| `npx.cmd playwright test --config ../.runtime/admin-form-regression.playwright.config.ts --workers 1 --max-failures 4` | 37 admin cases passed; combined run: 38 passed, two customer-width failures |
| Original-dialog comparison with `baseline-dialog.playwright.config.ts` | The same two customer-width assertions failed with the unchanged dialog from HEAD |
| `dotnet build tests/be.AcceptanceHost/be.AcceptanceHost.csproj --configuration Release --artifacts-path .runtime/admin-form-build --verbosity minimal` from repository root | Passed, zero warnings/errors |
| `git diff --check` | Passed |

The 20 new browser cases cover all four dismissal methods, nested confirmation/focus, empty and server-side validation, duplicate submission, catalogue/count refresh, preserved filters, image upload/order/removal, video previews, failed-upload retry, edit persistence, 404/409 handling, long-table scroll retention, category hierarchy, collection membership across API pages, content, promotions and route cleanup. They assert zero catalogue reads while opening/typing. Existing admin regressions also verify authentication/MFA boundaries, navigation, media/variants, promotions, production, inventory, reporting and accessibility.

Chrome checks cover 1440px, 768px and 390px widths, keyboard cycling, automated WCAG 2.0/2.1 AA rules and a 450px-high mobile viewport resize. Screenshots were visually inspected. Local evidence: `.runtime/admin-form-product-1440.png`, `admin-form-product-768.png` and `admin-form-product-390.png`. Physical phones, real on-screen keyboards and manual screen-reader sessions were not tested.

Portable form-suite entry point with the repository's normal acceptance configuration: `npm.cmd run test:e2e -- --project staff e2e/admin/form-dialogs.spec.ts`.

## F. Existing limitations and remaining issues

1. The existing product-media service stores `MediaType = "image"`, and storage uses image validation/processing. The old UI offered video selection even though these APIs reject video uploads. Selection and video previews remain available; image upload persistence is verified. Video persistence requires a separate storage/API change and is not claimed as complete by this UI change.
2. Full lint remains blocked by the unrelated Instagram unused variable; the unrelated dropdown hook warning is also preserved.
3. Customer avatar/cart tests fail a 320px document-width assertion in the current storefront. Diagnostics measured overflow before opening a dialog; both failures reproduce with the original dialog. Their tests and the unrelated storefront layouts were preserved. No claim is made that the entire browser suite passes.
4. Collection selection retains the existing first-page product choices and additionally merges every current member, so metadata edits cannot silently drop members beyond that page.

## G. Important changed files

| Files | Purpose |
| --- | --- |
| `client/src/features/admin/shared/components/AdminFormDialog.jsx`, `adminFormDialog.css` | Shared form presentation, persistent actions and discard policy |
| `client/src/shared/components/ui/Dialog.jsx`, `dialogScrollLock.js`, `dialogScrollLock.test.js` | Native dialog extensions, nested scroll cleanup and regression coverage |
| `client/src/features/admin/features/products/ProductsPage.jsx`, `useProducts.js` | Catalogue state, dialog triggers and saved-item discovery |
| `ProductEditor.jsx`, `useProductForm.js`, `ProductForm.jsx`, `ProductMediaPicker.jsx`, `productForm.css` in that product directory | Isolated reusable product draft, complete fields, media selection and safe retries |
| `ProductTools.jsx`, `ProductMediaEditor.jsx`, `ProductVariantsEditor.jsx`, `useProductTools.js` | Media/variant dialog, pending controls, error retention and preview cleanup |
| Category, collection, content and promotion page/form/hook files | Migrate the existing workflows without changing their API contracts |
| Builder configuration page/hook and admin base styles | Preserve the dedicated workflow with persistent actions and draft protection |
| `client/e2e/admin/form-dialogs.spec.ts` | Twenty new browser acceptance cases |
| Existing admin `catalog`, `operations` and `navigation-loading` specs | Preserve their assertions using the current dialog actions and accessible labels |

## H. Branch integration

Before committing, `git pull --ff-only origin develop` confirmed the branch was current at `4013c98`. The saved working changes reapplied cleanly; no merge conflicts were found. Related files are grouped into a shared dialog infrastructure commit and an admin workflow migration commit, each including its regression coverage. No deployment was performed.
