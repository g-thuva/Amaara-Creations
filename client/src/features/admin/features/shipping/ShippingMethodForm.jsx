import { newMethod, numeric } from "@/features/admin/features/shipping/shippingForm.js";
export function ShippingMethodForm({
  resource,
  method,
  setMethod,
  pending,
  save,
}) {
  return (
    <section className="card">
      <div className="card-header">
        <h2>Delivery methods</h2>
        <button
          className="btn btn-outline-primary"
          onClick={() => setMethod(newMethod())}
        >
          New method
        </button>
      </div>
      <div className="card-body">
        <label>
          Edit delivery method
          <select
            className="form-control"
            value={method.id || ""}
            onChange={(e) =>
              setMethod(
                resource.data?.methods.find(
                  (m) => m.id === Number(e.target.value),
                ) || newMethod(),
              )
            }
          >
            <option value="">New method</option>
            {resource.data?.methods.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name}
              </option>
            ))}
          </select>
        </label>
        <form onSubmit={(e) => save(e, "method")} className="add-product-form">
          <div className="form-row">
            <label>
              Zone
              <select
                className="form-control"
                required
                value={method.shippingZoneId || ""}
                onChange={(e) =>
                  setMethod({ ...method, shippingZoneId: e.target.value })
                }
              >
                <option value="">Choose zone</option>
                {resource.data?.zones.map((z) => (
                  <option key={z.id} value={z.id}>
                    {z.name}
                  </option>
                ))}
              </select>
            </label>
            {["name", "code", "currency"].map((field) => (
              <label key={field}>
                {field}
                <input
                  className="form-control"
                  required
                  value={method[field]}
                  maxLength={
                    field === "currency" ? 3 : field === "code" ? 50 : 120
                  }
                  pattern={field === "code" ? "[A-Z0-9_-]+" : undefined}
                  onChange={(e) =>
                    setMethod({ ...method, [field]: e.target.value })
                  }
                />
              </label>
            ))}
          </div>
          <div className="form-row">
            {numeric.map((field) => (
              <label key={field}>
                {field.replace(/([A-Z])/g, " $1")}
                <input
                  className="form-control"
                  type="number"
                  min="0"
                  step={
                    field.includes("Days") || field === "displayOrder"
                      ? 1
                      : 0.01
                  }
                  value={method[field] ?? ""}
                  onChange={(e) =>
                    setMethod({
                      ...method,
                      [field]:
                        e.target.value === "" ? null : Number(e.target.value),
                    })
                  }
                />
              </label>
            ))}
          </div>
          <div className="form-row">
            {["isActive", "isPickup", "isDevelopmentValue"].map((field) => (
              <label key={field}>
                <input
                  type="checkbox"
                  checked={method[field]}
                  onChange={(e) =>
                    setMethod({ ...method, [field]: e.target.checked })
                  }
                />
                {field.replace(/([A-Z])/g, " $1")}
              </label>
            ))}
          </div>
          <label>
            Pickup location
            <input
              className="form-control"
              required={method.isPickup}
              value={method.pickupLocation || ""}
              maxLength="500"
              onChange={(e) =>
                setMethod({ ...method, pickupLocation: e.target.value })
              }
            />
          </label>
          <button className="btn btn-primary" disabled={pending}>
            Save delivery method
          </button>
        </form>
      </div>
    </section>
  );
}
