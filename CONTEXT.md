# Amaara Creations domain glossary

The terms below describe the commerce business. Module ownership and implementation decisions live in the architecture documentation.

## User
An identity account used for authentication. A User can have customer and staff roles simultaneously.

## Customer
A User with the customer role who owns shopping, address, design and order information. Customer is a business role, not a separate identity account.

## Order
A durable record of a checkout with captured item, amount and delivery facts. Creating an Order does not establish that payment is settled or goods are fulfilled.

## Proof Revision
A version of the production proof supplied for a Custom Design. A customer responds to the current revision; an older revision cannot approve a newer proof.

## Product
A sellable catalog item presented to customers. A Product has mutable catalog information, so Orders keep snapshots of product facts that must remain historically accurate.

## Category
A durable catalog grouping for Products. Category replaces long-term dependence on the legacy free-text product category while preserving the legacy value during migration.

## Collection
A marketing or merchandising grouping of Products, separate from category hierarchy.

## Product Variant
A purchasable stock-bearing option of a Product. Variant inventory is protected with optimistic concurrency.

## Product Media
Metadata that points to externally stored or static product media. Media files are not stored as large database blobs.

## Address
A user-owned delivery/contact address prepared for later account workflows.

## Custom Design
A saved custom-product design snapshot. It may belong to a signed-in user or remain unassigned for later guest support.

## Refresh Session
A persistent authentication session record that stores hashed refresh-token data, never raw refresh tokens.

## Order Item Snapshot
Immutable order-line facts captured at checkout so historical Orders do not depend on mutable Product records.

## Payment
A single attempt to pay an Order. An Order can retain several attempts, while only verified settled funds count as paid.

## Shipment
A delivery or pickup record for an Order, with its own fulfilment state independent of payment, proof, and production.

## Promotion
A reusable discount offered through a coupon code. Its redemption becomes committed when payment is confirmed.

## Inventory Reservation
A temporary allocation of standard-product stock to an unpaid Order. It is consumed once after payment or released when payment fails, is cancelled, or safely expires.

## Refund
A credit of previously settled funds to the customer. A refund is separate from the physical return of goods.

## Checkout Quote
A current calculation of items, discount, delivery, tax, fees, and total in the store currency. It is refreshed before a pending Order is created.

## Audit Log
A privileged/business change record that must not contain secrets, passwords, raw tokens, or payment-card data.

## Inventory Movement

An append-only change to available base or variant stock. Checkout reservation reduces available stock, release restores it, and consumption records sale of previously reserved goods without subtracting available stock twice. On-hand is available plus currently reserved quantity. Manual changes require an expected available balance, a reason, a staff actor and coordinated stock changes.

## Staff Permission

A server authorization policy distinct from admin navigation. Support can inspect customers/orders without financial, staff, settings, report or audit permissions. Catalog, production, marketing and analyst roles receive scoped access. Super Admin alone changes staff/settings. Legacy Admin retains existing order/financial operations.

## Verified Purchase Review

A review linked by the server to the author's eligible fulfilled, settled order item. Browser verification flags are ignored. New/edited content is Pending until moderated; public responses contain no customer email, account identifier or private order linkage.
