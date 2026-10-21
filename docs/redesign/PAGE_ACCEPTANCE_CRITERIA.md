# Page acceptance criteria and scope disposition

| Page / feature | Required behavior | Implementation / remaining gate |
| --- | --- | --- |
| Header/footer | Logo home, search, cart/account, touch and keyboard navigation, genuine contacts/policies | Existing working shell refined; no staff link. Help and saved work added. Policy/contact facts require configured owner content |
| Home | Readable split hero, genuine data, useful empty shop | Editorial study + CMS imagery boundary; published categories/products only; no fabricated social proof |
| Shop/search | Shareable filters, matching count, distinct zero inventory/no matches | Backend `publishedCount` only counts visible currency/published rows; sidebar and toolbar hidden for empty inventory |
| Product | Gallery, actual variants/stock/currency and valid purchase path | Selected variants now persist as distinct cart lines, use current server prices, reserve their own stock and survive failed-payment release; standard-product stock remains separate |
| Designer | Shape/ratio/text/artwork, one quoted total, private saved work | Versioned layout supports fit/crop/zoom/rotation/text placement/transparency, private artwork restore, resolution feedback, PNG preview export, stop/retry transfers and local-draft conflict recovery. Production bleed/cut paths and unprovided fonts/colours require the approved proof |
| Cart/checkout | Authoritative totals, configured methods, idempotency and truthful payment state | Cart drawer/full page, selected options, protected artwork thumbnails and removal restoration are implemented. Existing transaction/payment/refund workflows remain validated; guest/COD/transfer and a new LKR gateway require approved business rules/provider onboarding |
| Orders/proofs | Owner authorization, exact revision approval and production state | Existing protected workflows retained; regression suites verify transitions and private attachments |
| Login/register/reset | Password managers, toggles, passphrases and safe recovery | Shared password controls + server validator; duplicate registration returns generic success; Identity reset tokens expire in 30 minutes |
| Profile/addresses | Real data, safe updates, photo fallback, status | Existing CRUD plus initials, dirty/unload state and cancellable crop/upload with retry. Verified email change is implemented and disabled until an approved sender is configured |
| Account security | Password change, MFA, device controls and truthful privacy actions | TOTP enrollment/replacement/replay prevention, hashed one-use recovery, device listing/revocation, logout-all, reauthenticated account export and deletion-review requests are implemented. Actual erasure remains subject to approved retention review |
| Help/contact/about | Useful truthful guidance and verified channels | New help page answers 11 practical questions. Business story/contact form/policies need verified operator content and a real processing service |
| Staff | Independent artifact, recent MFA, role policies, audit | Existing modules plus quote/privacy-review queues are protected by backend policies and capability gating. Administration remains disabled; real staff provisioning and the protected identity-aware host remain operator release gates |
| SEO/performance | Published-resource metadata, no private indexing and measured budgets | Existing resource SEO retained; help metadata added; staff noindex, no source maps, bundle isolation. SSR/prerender, real-image optimization and field vitals need deployment validation |

## Quality gates

Run the baseline frameworks, not a replacement harness: .NET unit/API/architecture/SQL tests; Vitest; ESLint; TypeScript; separate Vite builds; manifest/code isolation check; Playwright storefront and staff projects; axe; production-browser lab; dependency and secret checks; EF pending-model check. See TEST_AND_RELEASE_REPORT for actual results. A successful automated run is not a substitute for screen-reader, security-review, real gateway or production content approval.
