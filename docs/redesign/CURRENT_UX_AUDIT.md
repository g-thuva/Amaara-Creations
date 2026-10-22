# Current UX audit and prioritized issue register

Baseline clean commit: `8c7bf54`; local and fetched `origin/develop` matched. Route evidence below comes from source and the acceptance host, not the inaccessible public deployment. Before images are in `evidence/before/`; redesigned captures are in `evidence/after/`. Each image uses test-only products with synthetic image pixels, not genuine business inventory.

| Priority / severity | Issue and evidence | Impact | Implemented direction | Regression evidence |
| --- | --- | --- | --- | --- |
| P0 / high | `app/routes/AppRoutes.jsx` imported staff routes into the shop build | Staff code delivered publicly | Remove that dependency; independent staff build with server MFA gate | Build manifest isolation checker and direct public deep-link browser test |
| P0 / high | Identity accepted six-character new passwords; fields lacked toggles | Weak choices and entry errors | Shared 15–128 code-point validator and accessible PasswordField | Backend password vectors, browser toggles/autocomplete |
| P0 / high | Access JWTs were not bound to active refresh families/security stamps | Logout/password changes left access tokens valid until expiry | Check family and stamp on every bearer request | Logout, signature and demotion HTTP tests |
| P0 / high | Existing role policies lacked second-factor assurance | A stolen staff password could grant operational access | Default-closed staff policies, verified TOTP, fixed 15-minute assurance, 5-minute sensitive policy | MFA positive/negative/expiry/lockout tests |
| P1 / medium | Header and account sidebar rendered Admin panel for administrative users shopping | Mixed entry points | Remove staff links for every shop user | Storefront link scan for a staff-role shopper |
| P1 / medium | Home hero used a large dark slab and heavy typography | Weak product explanation and hierarchy | Split editorial hero, labeled original design studies and configured CMS photos | Home 390/768/1440 screenshots |
| P1 / high | `BuilderPreview.jsx` used an irregular border radius for every shape; plural was unconditional | Wrong production expectation | Shape geometry, bounded aspect ratio, singular/plural, illustrative guides and contour caveat | Geometry vectors and browser preview bounds |
| P1 / high | Reopened design metadata did not reload its private artwork preview | Saved work appeared incomplete | Authorized private blob restoration, no public artwork URL | Save/upload/reload browser test |
| P1 / medium | Products page displayed filters without published inventory | Empty-shop dead end | Backend published-count metadata; distinguish zero inventory from filtered-out results | Both empty-state browser paths |
| P1 / medium | Profile used a clipped general-purpose image placeholder | Confusing account identity | Initial-based avatar, inline navigation, disabled unchanged save and unload warning | Customer lifecycle and responsive checks |
| P2 / medium | Footer columns were sparse without configured contacts | Unbalanced layout and weak support discovery | Responsive layout, working help/order/design links, configured policy URLs | Footer unit check and screenshots |
| P2 / medium | Contact/policy/photos and payment/delivery facts unavailable | Cannot truthfully complete business-facing copy | Preserve configured content only; release gates in OWNER_DECISIONS | No synthetic business content seeded |

The PDF's clipped-hero screenshot was not available as a separate file. Local captures did not reproduce clipped eyebrow text at the checked widths; the oversized layout was reproducible. Admin links in source were role-conditional, so this audit does **not** claim anonymous visitors had open admin APIs.

## Route regression checklist

- Home: one visible main heading, unclipped copy, deliberate material illustration, real content only.
- Shop: working search/filter URLs, current count, zero-inventory distinction, no hidden offscreen interactions.
- Product: actual price/currency, configured variants, gallery, genuine review data and correct purchase action.
- Designer: rectangle/round geometry, extreme aspect ratios, quantity grammar, current server quote, private saved art, conflict/error recovery.
- Login/register/reset/security: independent toggles, autocomplete, password-manager paste, inline feedback and account-level throttling.
- Account: owner-scoped data, readable nav, photo fallback and saved-state feedback.
- Checkout: persisted payment state, idempotent callbacks and configured methods; provider simulations are labeled tests.
- Staff: separate artifact/origin, MFA before module load, permission restrictions and immutable audit evidence.

Further release limitations and actual observed test counts belong in TEST_AND_RELEASE_REPORT.md; screenshots alone do not prove functional controls.
