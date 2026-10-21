# Page acceptance criteria and scope disposition

| Page / feature | Required behavior | Implementation / remaining gate |
| --- | --- | --- |
| Header/footer | Logo home, search, cart/account, touch and keyboard navigation, genuine contacts/policies | Existing working shell refined; no staff link. Help and saved work added. Policy/contact facts require configured owner content |
| Home | Readable split hero, genuine data, useful empty shop | Editorial study + CMS imagery boundary; published categories/products only; no fabricated social proof |
| Shop/search | Shareable filters, matching count, distinct zero inventory/no matches | Backend `publishedCount` only counts visible currency/published rows; sidebar and toolbar hidden for empty inventory |
| Product | Gallery, actual variants/stock/currency and valid purchase path | Existing flow retained and tested. Variant purchase remains unavailable where backend cart does not support it; clear contact action remains. No claim this limitation is solved |
| Designer | Shape/ratio/text/artwork, one quoted total, private saved work | Geometry, quantity grammar, restored private art and session draft status implemented. Advanced crop/zoom/bleed export, unsupported production fonts/colors and quote-request workflow remain product work; no decorative controls added |
| Cart/checkout | Authoritative totals, configured methods, idempotency and truthful payment state | Existing workflows retained. Guest checkout, Sri Lankan gateway onboarding, COD/transfer and approved district rules are not invented or enabled |
| Orders/proofs | Owner authorization, exact revision approval and production state | Existing protected workflows retained; regression suites verify transitions and private attachments |
| Login/register/reset | Password managers, toggles, passphrases and safe recovery | Shared password controls + server validator; duplicate registration returns generic success; Identity reset tokens expire in 30 minutes |
| Profile/addresses | Real data, safe updates, photo fallback, status | Existing CRUD preserved; initials and dirty/unload feedback added. Crop UI and verified email-change workflow are not implemented |
| Account security | Password change, logout-all and accurate controls | Existing actions retained and access-token revocation strengthened. Customer MFA, full session list and privacy export/deletion flows remain unimplemented and are not rendered as working buttons |
| Help/contact/about | Useful truthful guidance and verified channels | New help page answers 11 practical questions. Business story/contact form/policies need verified operator content and a real processing service |
| Staff | Independent artifact, recent MFA, role policies, audit | Existing modules preserved behind default-disabled server controls and verified capability gate. Owner provisioning, replay-safe MFA enrollment/recovery and gateway deployment remain blocked release work |
| SEO/performance | Published-resource metadata, no private indexing and measured budgets | Existing resource SEO retained; help metadata added; staff noindex, no source maps, bundle isolation. SSR/prerender, real-image optimization and field vitals need deployment validation |

## Quality gates

Run the baseline frameworks, not a replacement harness: .NET unit/API/architecture/SQL tests; Vitest; ESLint; TypeScript; separate Vite builds; manifest/code isolation check; Playwright storefront and staff projects; axe; production-browser lab; dependency and secret checks; EF pending-model check. See TEST_AND_RELEASE_REPORT for actual results. A successful automated run is not a substitute for screen-reader, security-review, real gateway or production content approval.
