# Cart variant migration

`20261021225400_BoutiqueCartVariants` adds a nullable `CartItems.ProductVariantId`, a restricted foreign key and an option-aware unique index. Existing cart rows remain standard-product rows with null variant IDs. Existing order, reservation, payment and inventory schemas already support variants. Historical migrations are unchanged; the current EF snapshot and its reviewed compatibility hash advance with this migration.

The migration was generated without applying it to an existing development or business database. Fresh disposable SQL tests verify historical cart preservation, independent base/variant balances and concurrent checkout stock protection. The migration's Down operation refuses rollback while any variant cart line exists, so a rollback cannot silently discard a buyer's selected option. Resolve those lines through a reviewed business process or prefer a forward fix. Do not delete them as an automatic rollback step.

Before staging or production: take and verify an encrypted backup through the existing runbook, apply the reviewed migration in staging, exercise both standard and variant checkout with genuine configured inventory, and obtain deployment approval. A frontend rollback must remain compatible with the migrated API/database. No production migration or deployment is included in this implementation.
