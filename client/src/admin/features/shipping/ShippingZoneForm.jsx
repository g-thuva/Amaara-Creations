import { newZone } from "./shippingForm.js";
export function ShippingZoneForm({ resource, zone, setZone, pending, save }) {
  return (
    <section className="card">
      <div className="card-header">
        <h3>Delivery zones</h3>
        <button
          className="btn btn-outline-primary"
          onClick={() => setZone(newZone())}
        >
          New zone
        </button>
      </div>
      <div className="card-body">
        <label>
          Edit zone
          <select
            className="form-control"
            value={zone.id || ""}
            onChange={(e) =>
              setZone(
                resource.data?.zones.find(
                  (z) => z.id === Number(e.target.value),
                ) || newZone(),
              )
            }
          >
            <option value="">New zone</option>
            {resource.data?.zones.map((z) => (
              <option key={z.id} value={z.id}>
                {z.name}
              </option>
            ))}
          </select>
        </label>
        <form onSubmit={(e) => save(e, "zone")} className="add-product-form">
          <div className="form-row">
            {["name", "country", "region", "postalCodePrefix"].map((field) => (
              <label key={field}>
                {field.replace(/([A-Z])/g, " $1")}
                <input
                  className="form-control"
                  required={["name", "country"].includes(field)}
                  value={zone[field] || ""}
                  maxLength={
                    field === "name"
                      ? 120
                      : field === "country"
                        ? 80
                        : field === "region"
                          ? 100
                          : 30
                  }
                  onChange={(e) =>
                    setZone({ ...zone, [field]: e.target.value })
                  }
                />
              </label>
            ))}
          </div>
          <div className="form-row">
            {[
              "isActive",
              "requiresPhone",
              "requiresRegion",
              "requiresPostalCode",
            ].map((field) => (
              <label key={field}>
                <input
                  type="checkbox"
                  checked={zone[field]}
                  onChange={(e) =>
                    setZone({ ...zone, [field]: e.target.checked })
                  }
                />
                {field.replace(/([A-Z])/g, " $1")}
              </label>
            ))}
          </div>
          <button className="btn btn-primary" disabled={pending}>
            Save zone
          </button>
        </form>
      </div>
    </section>
  );
}
