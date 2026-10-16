import { ConfirmDialog } from "../../../components/ui/ConfirmDialog.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { Pagination } from "../../shared/components/Pagination.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
import { useInventory } from "./useInventory.js";
import { movementPageSize } from "./inventoryApi.js";
import { StockBalancesTable } from "./StockBalancesTable.jsx";
import { InventoryAdjustmentForm } from "./InventoryAdjustmentForm.jsx";
import { InventoryMovementsTable } from "./InventoryMovementsTable.jsx";
export default function InventoryPage() {
  const inventory = useInventory();
  const { resource, movements, selected, intent, setIntent, mutation } =
    inventory;
  return (
    <div className="admin-page">
      <h1>Inventory ledger</h1>
      <p>
        On hand includes reserved goods. Available stock is protected by
        checkout reservations.
      </p>
      <label>
        Search product or SKU
        <input
          className="form-control"
          value={inventory.search}
          onChange={(e) => {
            inventory.setSearch(e.target.value);
            inventory.setPage(1);
          }}
        />
      </label>
      <MutationFeedback {...mutation} />
      <ResourceState resource={resource}>
        <StockBalancesTable
          resource={resource}
          selectBalance={inventory.selectBalance}
        />
        <Pagination
          page={inventory.page}
          setPage={inventory.setPage}
          data={resource.data}
          size={20}
          label="Stock balance pages"
        />
      </ResourceState>
      {selected && (
        <InventoryAdjustmentForm
          selected={selected}
          pending={mutation.pending}
          submit={inventory.submit}
          close={() => inventory.selectBalance(null)}
        />
      )}
      <h2>Inventory movements{selected ? ` · ${selected.product}` : ""}</h2>
      <ResourceState resource={movements}>
        <InventoryMovementsTable movements={movements} />
        <Pagination
          page={inventory.movementPage}
          setPage={inventory.setMovementPage}
          total={movements.data?.totalCount || 0}
          size={movementPageSize}
          label="Inventory movement pages"
        />
      </ResourceState>
      <ConfirmDialog
        open={Boolean(intent)}
        onClose={() => setIntent(null)}
        title="Confirm inventory adjustment"
        pending={mutation.pending}
        onConfirm={inventory.confirmAdjustment}
      >
        Change available quantity by {intent?.quantityDelta}. Reason:{" "}
        {intent?.reason}
      </ConfirmDialog>
    </div>
  );
}
