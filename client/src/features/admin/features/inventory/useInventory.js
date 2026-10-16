import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { inventoryApi } from "@/features/admin/features/inventory/inventoryApi.js";
export function useInventory() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState(null);
  const [intent, setIntent] = useState(null);
  const resource = useResource(
    (signal) => inventoryApi.getBalances({ page, search }, signal),
    JSON.stringify([page, search]),
  );
  const [movementPage, setMovementPage] = useState(1);
  const movements = useResource(
    (signal) =>
      inventoryApi.getMovements(
        { productId: selected?.productId, page: movementPage },
        signal,
      ),
    JSON.stringify([selected?.productId, movementPage]),
  );
  const selectBalance = (balance) => {
    setSelected(balance);
    setMovementPage(1);
  };
  const mutation = useAdminMutation(() => {
    resource.reload();
    movements.reload();
  });
  const submit = (event) => {
    event.preventDefault();
    const form = Object.fromEntries(new FormData(event.currentTarget));
    setIntent({
      variantId: selected.variantId,
      expectedAvailable: selected.available,
      quantityDelta: Number(form.delta),
      reason: form.reason,
      lowStockThreshold: Number(form.threshold),
    });
  };
  const confirmAdjustment = async () => {
    const ok = await mutation.run(
      () => inventoryApi.adjustStock(selected.productId, intent),
      "Inventory adjustment recorded.",
    );
    setIntent(null);
    if (ok) selectBalance(null);
  };
  return {
    page,
    setPage,
    search,
    setSearch,
    selected,
    intent,
    setIntent,
    resource,
    movements,
    movementPage,
    setMovementPage,
    selectBalance,
    mutation,
    submit,
    confirmAdjustment,
  };
}
