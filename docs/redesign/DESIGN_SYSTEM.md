# Boutique design system

Source of truth: shared `tokens.css` plus storefront-scoped `studio.css`. Operational staff tokens remain in their existing feature boundary. The existing A-mark/logo is retained.

## Foundations

Canvas `#FCFAF7`; surface white; warm surface `#F5F0EA`; ink `#102744`; navy `#19385F`; interactive blue `#2466A7`; body `#344458`; secondary `#596879`; warm accent `#BF8B61`. Accent is decorative and must not be the sole indicator of state.

Calculated WCAG relative-luminance contrast ratios: ink/canvas 14.44:1, body/canvas 9.54:1, secondary/canvas 5.48:1, action blue/canvas 5.70:1, white/navy 11.85:1. These token pairs do not prove all rendered combinations pass; axe and visual checks remain necessary.

Body/UI use locally served Inter with system fallbacks. The designer’s Poppins semibold subset is also local; both retain SIL OFL licenses under public/fonts. Editorial hero and page titles use a system serif so the layout remains usable when font requests fail. The designer keeps its configured Poppins/Inter mapping separate from editorial typography. Default headings are restrained; hero ranges 2.6–4.2rem. Containers cap at 1280px. Spacing uses the existing 4/8px scale. Inputs/buttons aim for 44px or more, with visible focus.

## Reusable components and states

| Component | Existing/new implementation | State requirements |
| --- | --- | --- |
| Button/Icon actions/links | Existing shared primitives | Named action; focus, hover, disabled, pending; no decorative dead controls |
| Forms | Shared CSS and existing feature forms | Persistent labels, autocomplete, native constraints, server feedback and error descriptions |
| PasswordField | New shared component | Password by default, independent accessible toggle, aria-controls/pressed, correct autocomplete, Caps Lock hint; hide after 30s or window blur; passphrase guidance |
| Quantity/select/options | Existing quantity and server-driven selectors | Bounded values, keyboard use, unavailable options excluded |
| Dialog/drawer/confirmation | Existing native dialog primitive | Focus containment/return, Escape, named heading, clear destructive confirmation |
| Toast/alert/error/skeleton | Existing shared feedback | Status or alert role, retry where meaningful, no false saved/payment confirmation |
| Product/media/gallery | Existing catalogue components | Genuine data, explicit dimensions, alt text, fallback and loading behavior |
| Quote/cart/purchase | Existing server-backed features | Currency-labeled amounts, price refresh, quantity, pending/declined/paid distinctions |
| Preview/guides | Shape helper and BuilderPreview | Correct bounding ratio and configured shape; contour uncertainty stated; illustrative guides labeled |
| Profile | Initial-based fallback and existing upload | No clipped image placeholder; dirty save state; unloading warning; safe failure feedback |
| Staff tables/pagination | Existing operational feature components | Role-specific rows/actions, labels, independent pagination, mutation feedback and audit |

Motion stays short and purposeful; reduced-motion disables nonessential animation/transitions. No new scroll effects or autoplay are introduced. Existing CMS carousel behavior remains compatible, including its pause control and reduced-motion handling.

## Responsive and accessibility review

Shop shell and feature pages must reflow at 320/360/390/430/768/1024/1440px. At narrow widths the header uses the existing keyboard-accessible drawer; product filters use a drawer only when inventory exists. Account icon-label navigation remains inline. Designer bounds both tall and wide artwork within a square stage without squeezing its ratio.

Manual checks still required: NVDA/VoiceOver journeys, 200% zoom and user text scaling, Sinhala/Tamil content expansion, actual devices and meaningful image descriptions from the owner. This is an implementation guide, not a WCAG certification.
