import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
export function RefundHistory({ payment, busy, reconcile }) {
  return (
    <>
      {" "}
      <h4>Refund history</h4>
      {payment.refunds.length ? (
        <ul>
          {payment.refunds.map((r) => (
            <li key={r.id}>
              {checkoutMoney(r.amount, r.currency)} — {r.status}: {r.reason}
              {r.status === "Pending" && (
                <button
                  className="btn btn-outline-primary"
                  disabled={busy}
                  onClick={() => reconcile(r.id)}
                >
                  Reconcile refund
                </button>
              )}
            </li>
          ))}
        </ul>
      ) : (
        <p>No refunds.</p>
      )}
    </>
  );
}
