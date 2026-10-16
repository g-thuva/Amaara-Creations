import { localDate } from "@/features/admin/features/promotions/promotionForm.js";
export function PromotionForm({ editing, setEditing, submit, pending }) {
  return (
    <form
      key={editing.id || "new"}
      className="card card-body add-product-form"
      onSubmit={submit}
    >
      <h2>{editing.id ? "Edit promotion" : "New promotion"}</h2>
      <p>
        Dates use your local time and are stored in UTC. Checkout validates
        every redemption.
      </p>
      <div className="form-row">
        <label>
          Code
          <input
            name="code"
            className="form-control"
            maxLength="40"
            pattern="[A-Za-z0-9_-]+"
            defaultValue={editing.code}
            required
          />
        </label>
        <label>
          Name
          <input
            className="form-control"
            name="name"
            maxLength="160"
            defaultValue={editing.name}
            required
          />
        </label>
        <label>
          Currency
          <select
            className="form-control"
            name="currency"
            defaultValue={editing.currency}
            required
          >
            <option value="">Choose currency</option>
            {["LKR", "CHF", "USD", "EUR", "GBP"].map((c) => (
              <option key={c}>{c}</option>
            ))}
          </select>
        </label>
      </div>
      <div className="form-row">
        <label>
          Discount type
          <select
            className="form-control"
            name="discountType"
            defaultValue={
              Number(editing.discountType) === 0 ? "Percentage" : "FixedAmount"
            }
          >
            <option>FixedAmount</option>
            <option>Percentage</option>
          </select>
        </label>
        <label>
          Discount value
          <input
            className="form-control"
            name="discountValue"
            type="number"
            min="0.01"
            step="0.01"
            defaultValue={editing.discountValue}
            required
          />
        </label>
        <label>
          Starts
          <input
            className="form-control"
            name="startsAt"
            type="datetime-local"
            defaultValue={localDate(editing.startsAt)}
            required
          />
        </label>
        <label>
          Ends (optional)
          <input
            className="form-control"
            name="endsAt"
            type="datetime-local"
            defaultValue={localDate(editing.endsAt)}
          />
        </label>
      </div>
      <div className="form-row">
        {[
          ["minimumSpend", "Minimum order"],
          ["maximumDiscount", "Maximum discount"],
          ["usageLimit", "Usage limit"],
          ["perCustomerUsageLimit", "Per-customer limit"],
        ].map(([key, label]) => (
          <label key={key}>
            {label} (optional)
            <input
              className="form-control"
              name={key}
              type="number"
              min={key.includes("Limit") ? "1" : "0"}
              step={key.includes("Limit") ? "1" : "0.01"}
              defaultValue={editing[key] ?? ""}
            />
          </label>
        ))}
      </div>
      {[
        ["isActive", "Active"],
        ["appliesToStandard", "Standard items"],
        ["appliesToCustom", "Custom items"],
      ].map(([key, label]) => (
        <label key={key}>
          <input type="checkbox" name={key} defaultChecked={editing[key]} />{" "}
          {label}
        </label>
      ))}
      <button className="btn btn-primary" disabled={pending}>
        Save promotion
      </button>
      <button
        type="button"
        className="btn btn-outline-primary"
        onClick={() => setEditing(null)}
      >
        Cancel
      </button>
    </form>
  );
}
