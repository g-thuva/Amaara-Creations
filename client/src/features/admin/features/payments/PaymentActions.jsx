import { ConfirmAction } from '@/features/admin/shared/components/ConfirmAction.jsx';
export function PaymentActions({
  payment,
  pending,
  loading,
  busy,
  refresh,
  reconcile,
  authorize,
}) {
  return (
    <div className="action-buttons">
      <button
        className="btn btn-outline-primary"
        disabled={pending || loading}
        onClick={refresh}
      >
        Refresh payment details
      </button>
      <ConfirmAction onConfirm={reconcile} pending={pending} title="Reconcile payment" detail="Check the provider’s verified state and apply any confirmed payment update to this order?" destructive={false}><button
        className="btn btn-outline-primary"
        disabled={busy}
      >
        Reconcile payment
      </button></ConfirmAction>
      {payment.status === "Authorized" &&
        ["settle", "cancel"].map((action) => (
          <button
            className="btn btn-primary"
            key={action}
            disabled={busy}
            onClick={() => authorize(action)}
          >
            {action === "settle"
              ? "Settle authorized payment"
              : "Reverse authorization"}
          </button>
        ))}
    </div>
  );
}
