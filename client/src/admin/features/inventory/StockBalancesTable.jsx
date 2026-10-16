import { DataTable } from "../../shared/components/DataTable.jsx";
export function StockBalancesTable({ resource, selectBalance }) {
  return (
    <DataTable
      caption="Stock balances"
      rows={resource.data?.items || []}
      rowKey={(r) => `${r.productId}-${r.variantId}`}
      columns={[
        { key: "product", label: "Product" },
        { key: "variant", label: "Variant" },
        { key: "sku", label: "SKU" },
        { key: "onHand", label: "On hand" },
        { key: "reserved", label: "Reserved" },
        {
          key: "available",
          label: "Available",
          render: (r) => (
            <>
              {r.available}
              {r.available <= r.lowStockThreshold && " · Low stock"}
            </>
          ),
        },
        { key: "lowStockThreshold", label: "Low-stock threshold" },
        {
          key: "action",
          label: "Action",
          render: (r) => (
            <button
              className="btn btn-outline-primary"
              onClick={() => selectBalance(r)}
            >
              Adjust {r.sku || r.product}
            </button>
          ),
        },
      ]}
    />
  );
}
