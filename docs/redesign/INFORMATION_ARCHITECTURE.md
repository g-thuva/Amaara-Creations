# Information architecture

The logo returns home. First-level links: Shop, Custom design, About, Help. Contact is available from Help and footer. Categories and collections are published-backend driven; do not invent unsupported occasions or product families.

Public routes: `/`, `/products`, `/products/:id`, `/custom`, `/about`, `/contact`, `/help`, `/pages/:slug`, authentication/recovery routes. Search/filter/sort use existing product query parameters. Unknown paths show the same helpful 404, including staff-looking shop URLs.

Protected customer routes: `/profile`, `/addresses`, `/orders`, `/orders/:id`, `/wishlist`, `/cart`, `/checkout`, `/custom/:id`, `/account/designs`, `/account/requests`, proof review and `/account/security`. Existing aliases remain supported. Customer navigation includes profile, orders, addresses, saved work, requests, wishlist, security and sign out; no staff entry, even when that account has a staff role.

Staff routes remain `/admin/*` **only in the independently built staff application**. The login shell verifies backend capabilities before importing operational routes. Staff artifacts must be hosted separately; a protected gateway is required if even staff JavaScript must be unavailable to anonymous requests.

Footer: brand summary; shop/company paths; customer-care paths; genuine contact/social details where configured. Policy links appear only when their public URL settings are configured. No nonfunctional newsletter or unsupported guest tracking.

Routing mode and production host are unchanged by this local task. Browser-history fallback, existing emailed hash-link compatibility and approved canonical base remain deployment concerns in the existing architecture/runbooks.
