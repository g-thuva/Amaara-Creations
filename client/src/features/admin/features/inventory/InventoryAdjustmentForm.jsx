export function InventoryAdjustmentForm({ selected, pending, submit, close }) {
  return (
    <section className="card">
      <h2>
        Adjust {selected.product} · {selected.variant}
      </h2>
      <p>Available when selected: {selected.available}</p>
      <form
        className="card-body add-product-form"
        key={`${selected.productId}-${selected.variantId}-${selected.available}`}
        onSubmit={submit}
      >
        <label>
          Quantity change
          <input
            className="form-control"
            name="delta"
            type="number"
            step="1"
            required
          />
        </label>
        <label>
          Low-stock threshold
          <input
            className="form-control"
            name="threshold"
            type="number"
            min="0"
            defaultValue={selected.lowStockThreshold}
            required
          />
        </label>
        <label>
          Reason
          <textarea
            className="form-control"
            name="reason"
            maxLength="300"
            required
          />
        </label>
        <button className="btn btn-primary" disabled={pending}>
          Review adjustment
        </button>
        <button
          type="button"
          className="btn btn-outline-primary"
          onClick={() => close()}
        >
          Close adjustment
        </button>
      </form>
    </section>
  );
}
