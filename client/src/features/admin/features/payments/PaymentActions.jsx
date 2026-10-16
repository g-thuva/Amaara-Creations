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
      <button
        className="btn btn-outline-primary"
        disabled={busy}
        onClick={reconcile}
      >
        Reconcile payment
      </button>
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
