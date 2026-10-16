export function ShipmentForm({
  shipment,
  shipmentState,
  nextShipment,
  pending,
  onSubmit,
}) {
  return (
    <section className="card">
      <div className="card-header">
        <h3>Shipment or pickup</h3>
      </div>
      <form
        className="card-body add-product-form"
        key={shipmentState}
        onSubmit={onSubmit}
      >
        <label>
          Next fulfilment status
          <select className="form-control" name="status">
            {nextShipment && (
              <option value={nextShipment}>{nextShipment}</option>
            )}
            {shipment && (
              <option value={shipmentState}>
                Correct tracking ({shipmentState})
              </option>
            )}
          </select>
        </label>
        <div className="form-row">
          <label>
            Carrier
            <input
              className="form-control"
              name="courier"
              defaultValue={shipment?.courier || ""}
              maxLength="120"
            />
          </label>
          <label>
            Tracking number
            <input
              className="form-control"
              name="trackingNumber"
              defaultValue={shipment?.trackingNumber || ""}
              maxLength="120"
            />
          </label>
          <label>
            Tracking URL
            <input
              className="form-control"
              name="trackingUrl"
              defaultValue={shipment?.trackingUrl || ""}
              type="url"
              pattern="https://.*"
              maxLength="500"
            />
          </label>
        </div>
        <label>
          Shipment notes
          <textarea className="form-control" name="notes" maxLength="500" />
        </label>
        <button className="btn btn-primary" disabled={pending}>
          Update fulfilment
        </button>
      </form>
    </section>
  );
}
