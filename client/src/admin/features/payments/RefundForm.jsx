import { checkoutMoney } from "../../../services/checkoutApi.js";
export function RefundForm({ payment, busy, onSubmit }) {
  return (
    <form className="add-product-form" onSubmit={onSubmit}>
      <h4>Refund settled funds</h4>
      <p>
        Available: {checkoutMoney(payment.refundableAmount, payment.currency)}.
        Physical stock is managed separately.
      </p>
      <div className="form-row">
        <label>
          Refund amount
          <input
            className="form-control"
            name="amount"
            type="number"
            step="0.01"
            min="0.01"
            max={payment.refundableAmount}
            required
            disabled={busy}
          />
        </label>
        <label>
          Refund reason
          <textarea
            className="form-control"
            name="reason"
            maxLength="500"
            required
            disabled={busy}
          />
        </label>
      </div>
      <button className="btn btn-primary" disabled={busy}>
        Review refund
      </button>
    </form>
  );
}
