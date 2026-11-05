# Role and permission matrix

Source of truth: `be/Application/Features/Identity/AppPolicies.cs`, not this prose or frontend role flags. Existing vocabulary is preserved rather than introducing overlapping Staff/Owner strings. SuperAdmin is the owner's elevated role.

CustomerAccess requires a storefront session with Customer membership and no privileged membership. Any administrative role takes precedence over Customer when choosing session scope. All accounts use the shared login page; privileged accounts additionally complete mandatory MFA and receive only management access. They cannot use customer-only account APIs. Their public browsing remains anonymous unless a separate eligible customer identity is explicitly signed in. See [shared login implementation](SHARED_LOGIN_AND_STAFF_MFA.md).

| Role | Scope |
| --- | --- |
| Anonymous | Deliberately public published catalogue/configuration/content; no customer records or staff policy |
| Customer | Own profile/addresses/orders/cart/wishlist/designs/artwork/proofs/reviews/requests; authenticated ownership checks; reauthentication for security, export, email and deletion-review actions |
| ProductionStaff | View/manage orders, production/proofs, inventory |
| CatalogManager | Catalogue/categories/collections/media/builder configuration and inventory |
| CustomerSupport | Customers, order viewing and review moderation |
| Marketing | Content, promotions and review moderation |
| Analyst | Reports |
| Admin | Operational scopes including customers, catalogue, content, inventory, payments/refunds, audit, reports; no owner-only settings or staff management |
| SuperAdmin | All configured administrative scopes, including settings and staff management |

Every staff policy additionally requires `Administration:Enabled=true`, authenticated current roles, server-bound verified MFA and an active privileged session (default thirty-minute meaningful idle limit and twelve-hour absolute limit). Refunds, staff changes, settings and report/export policies require assurance less than 5 minutes old. Denial is 401 when authentication is absent/revoked and 403 when an authenticated principal lacks permission/assurance. Public shop staff-looking paths are ordinary 404 content.

Quote requests/replies use ManageOrders. Privacy-request review uses the sensitive ManageStaff policy (SuperAdmin); ordinary operational staff cannot read those requests. Both API and UI enforce these scopes. Replies are customer-visible and do not create an order, take payment or erase records.

Signup never accepts an elevated role. Profile updates do not assign role or email. The bootstrap remains deliberately disabled and contains no credentials. Staff demotion revokes refresh families, so previously issued bound access tokens become unauthorized immediately. Ordinary staff APIs cannot edit/delete audit evidence.

Ownership and state tests already cover orders, payment details, artwork, proof revisions and reviews. The final security review must expand negative testing to every object type and legacy/static endpoint; a small representative test set is not a claim of exhaustive coverage.
