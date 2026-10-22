import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { ConfirmAction } from '@/features/admin/shared/components/ConfirmAction.jsx';
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
export function RefundHistory({ payment, busy, reconcile }) {
  return (
    <>
      {" "}
      <h3>Refund history</h3>
      {payment.refunds.length ? (
        <ul>
          {payment.refunds.map((r) => (
            <li key={r.id}>
              {checkoutMoney(r.amount, r.currency)} — <StatusChip status={r.status} />: {r.reason}
              {r.status === "Pending" && (
                <ConfirmAction onConfirm={() => reconcile(r.id)} pending={busy} title="Reconcile refund" detail="Check the verified provider outcome for this refund? This does not create another credit." destructive={false}><button
                  className="btn btn-outline-primary"
                  disabled={busy}
                >
                  Reconcile refund
                </button></ConfirmAction>
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
