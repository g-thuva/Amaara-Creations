import { money } from "../../../utils/storefront.ts";
import { numberFields } from "./builderForm.js";
export function BuilderPricingEditor({ draft, change, testQuote }) {
  return (
    <section className="card">
      <div className="card-header">
        <h3 className="card-title">Limits and pricing</h3>
      </div>
      <div className="card-body add-product-form">
        <div className="form-row">
          <label>
            Measurement unit
            <input
              className="form-control"
              value={draft.measurementUnit}
              onChange={(event) =>
                change("measurementUnit", event.target.value)
              }
            />
          </label>
          <label>
            Currency
            <input
              className="form-control"
              maxLength="3"
              value={draft.currency}
              onChange={(event) => change("currency", event.target.value)}
            />
          </label>
        </div>
        <div className="form-row">
          {numberFields.map((field) => (
            <label key={field}>
              {field.replace(/([A-Z])/g, " $1")}
              <input
                className="form-control"
                type="number"
                min="0"
                step={field.includes("Quantity") ? 1 : 0.01}
                value={draft[field]}
                onChange={(event) => change(field, event.target.value)}
              />
            </label>
          ))}
        </div>
        <div className="form-row">
          <label>
            <input
              type="checkbox"
              checked={draft.artworkRequired}
              onChange={(event) =>
                change("artworkRequired", event.target.checked)
              }
            />{" "}
            Artwork required
          </label>
          <label>
            <input
              type="checkbox"
              checked={draft.proofRequired}
              onChange={(event) =>
                change("proofRequired", event.target.checked)
              }
            />{" "}
            Proof required
          </label>
        </div>
        <label>
          Internal notes
          <textarea
            className="form-control"
            maxLength="500"
            value={draft.internalNotes || ""}
            onChange={(event) => change("internalNotes", event.target.value)}
          />
        </label>
        {testQuote && (
          <p className="badge-success">
            Test quote: {money(testQuote.subtotal)} ({testQuote.pricingVersion})
          </p>
        )}
      </div>
    </section>
  );
}
