# Role and permission matrix

Source of truth: `be/Application/Features/Identity/AppPolicies.cs`, not this prose or frontend role flags. Existing vocabulary is preserved rather than introducing overlapping Staff/Owner strings. SuperAdmin is the owner's elevated role.

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

Every staff policy additionally requires `Administration:Enabled=true`, authenticated current roles, verified MFA no older than 15 minutes, and an active session. Refunds, staff changes, settings and report/export policies require assurance less than 5 minutes old. Denial is 401 when authentication is absent/revoked and 403 when an authenticated principal lacks permission/assurance. Public shop staff-looking paths are ordinary 404 content.

Quote requests/replies use ManageOrders. Privacy-request review uses the sensitive ManageStaff policy (SuperAdmin); ordinary operational staff cannot read those requests. Both API and UI enforce these scopes. Replies are customer-visible and do not create an order, take payment or erase records.

Signup never accepts an elevated role. Profile updates do not assign role or email. The bootstrap remains deliberately disabled and contains no credentials. Staff demotion revokes refresh families, so previously issued bound access tokens become unauthorized immediately. Ordinary staff APIs cannot edit/delete audit evidence.

Ownership and state tests already cover orders, payment details, artwork, proof revisions and reviews. The final security review must expand negative testing to every object type and legacy/static endpoint; a small representative test set is not a claim of exhaustive coverage.
