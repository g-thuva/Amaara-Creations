# Amaara Creations Domain Glossary

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
A record of attempted or completed payment for an Order. Gateway integration is outside Foundation.

## Shipment
A fulfillment record for an Order. Carrier and tracking workflow is outside Foundation.

## Promotion
A reusable discount/coupon foundation. Discount application logic is outside Foundation.

## Audit Log
A privileged/business change record that must not contain secrets, passwords, raw tokens, or payment-card data.
