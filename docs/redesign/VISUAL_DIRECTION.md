# Visual direction and wireframes

## Direction A — studio worktable (selected)

Ivory canvas, navy typography, a medium-sized editorial serif headline paired with the existing sans UI. A split hero puts the primary sticker-design action beside original labeled material studies. Owner imagery replaces studies through existing CMS fields. Product data and category images come only from published catalogue records. Selection rationale: readable at mobile sizes, works honestly without inventory, supports everyday buyers and weddings equally, and has one clear task.

## Direction B — collection journal

Full-width genuine workshop photograph, a small inset headline and a restrained collection index beneath. Rejected for now: no verified workshop photography is available. An arbitrary stock photo would imply facts we cannot verify.

## Low fidelity wireframes (before page implementation)

Desktop home: `[logo | Shop / Custom / About / Help | search/account/cart]`, then `[eyebrow / headline / short copy / design CTA + shop link | material-study collage or CMS photograph]`, then real categories, published products or a custom-design invitation, process guidance, genuine CMS content, footer.

Mobile home: compact header / headline / actions / smaller collage / real categories / product list or custom-design invitation / process / footer. No clipped absolute-positioned headline.

Desktop shop: breadcrumb + intro / search and sort / filters only with inventory / genuine product grid or full-width empty state. Mobile uses a labeled filter drawer.

Designer desktop: title / grouped specification controls | large shape-faithful preview / one prominent quoted total / save status. Mobile: preview / controls / purchase action. Tall and wide previews stay inside a bounded stage without changing aspect ratio.

Account: greeting / horizontal icon-label sidebar | form and initial-based avatar / visible save feedback. Mobile: scrollable navigation then detail. Authentication: plain form, local password visibility controls, passphrase guidance and existing recovery links.

Admin: separate artifact, compact operational shell, recent-MFA authentication gate before loading operational modules, explicit permission checks on API and UI.

## Page and component inventory

Retain and refine Button, Icon, Dialog/Drawer, Toast, Skeleton, EmptyState, ErrorState, Media, Price, Quantity, ProductCard, pagination, gallery, account forms and responsive staff tables. Add shared PasswordField and geometry helper. Do not introduce an unrelated component framework or duplicate working primitives.

See DESIGN_SYSTEM.md for token/state rules, PAGE_ACCEPTANCE_CRITERIA.md for routes, and ASSET_REPLACEMENT_CHECKLIST.md for photography requirements.
