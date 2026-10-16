import { formatDateTime } from "../../shared/utils/formatDateTime.js";
import { usePaymentOperations } from "./usePaymentOperations.js";
import { paymentsApi } from "./paymentsApi.js";
import { checkoutMoney } from "../../../services/checkoutApi.js";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog.jsx";
import { PaymentActions } from "./PaymentActions.jsx";
import { RefundForm } from "./RefundForm.jsx";
import { RefundHistory } from "./RefundHistory.jsx";
export function PaymentAttemptPanel({ summary, changed }) {
  const {
    resource,
    payment,
    pending,
    notice,
    intent,
    setIntent,
    busy,
    run,
    prepareRefund,
    prepareAuthorization,
    refresh,
  } = usePaymentOperations(summary.id, changed);
  if (resource.error && !payment)
    return (
      <p role="alert">
        Payment details could not be loaded.{" "}
        <button onClick={resource.reload}>Refresh payment details</button>
      </p>
    );
  if (!payment) return <p role="status">Loading payment…</p>;
  return (
    <section className="card">
      <div className="card-header">
        <h3>Payment attempt {payment.attemptNumber || payment.id}</h3>
      </div>
      <div className="card-body">
        <p>
          {payment.provider}: {payment.status} ·{" "}
          {checkoutMoney(payment.amount, payment.currency)}
        </p>
        <p>Reference: {payment.providerReference}</p>
        <p>Transaction: {payment.providerTransactionId || "Not initialized"}</p>
        <p>
          Settled: {checkoutMoney(payment.settledAmount, payment.currency)} ·
          Refunded: {checkoutMoney(payment.refundedAmount, payment.currency)}
        </p>
        {payment.createdAt && (
          <p>Created: {formatDateTime(payment.createdAt)}</p>
        )}
        {payment.paidAt && <p>Confirmed: {formatDateTime(payment.paidAt)}</p>}
        {payment.providerAvailable === false && (
          <p role="status">
            Provider operations are unavailable until its private configuration
            and acceptance checks are complete.
          </p>
        )}
        {payment.providerMode && <p>Mode: {payment.providerMode}</p>}
        {payment.providerGatewayId && (
          <p>Gateway ID: {payment.providerGatewayId}</p>
        )}
        {payment.providerStatus && (
          <p>Provider state: {payment.providerStatus}</p>
        )}
        {payment.lastReconciledAt && (
          <p>Last checked: {formatDateTime(payment.lastReconciledAt)}</p>
        )}
        {payment.events?.length > 0 && (
          <>
            <h4>Provider event history</h4>
            <ul>
              {payment.events.map((event, index) => (
                <li key={`${event.processedAt}-${index}`}>
                  {event.providerStatus} — {formatDateTime(event.processedAt)}
                </li>
              ))}
            </ul>
          </>
        )}
        {notice && (
          <>
            <p role={notice.error ? "alert" : "status"}>{notice.text}</p>
            {notice.detail && <p>{notice.detail}</p>}
          </>
        )}
        {resource.loading && <p role="status">Refreshing payment details…</p>}
        {resource.error && (
          <p role="alert">
            Payment details could not be refreshed. Refresh before submitting
            another refund.
          </p>
        )}
        <p>
          Remaining refundable:{" "}
          {checkoutMoney(payment.refundableAmount, payment.currency)}
        </p>
        <PaymentActions
          payment={payment}
          pending={pending}
          loading={resource.loading}
          busy={busy}
          refresh={refresh}
          reconcile={() => run(() => paymentsApi.reconcilePayment(payment.id))}
          authorize={prepareAuthorization}
        />
        {payment.refundableAmount > 0 &&
          ["Paid", "PartiallyRefunded"].includes(payment.status) && (
            <RefundForm
              payment={payment}
              busy={busy}
              onSubmit={prepareRefund}
            />
          )}
        {payment.reconciliations?.length > 0 && (
          <>
            <h4>Reconciliation history</h4>
            <ul>
              {payment.reconciliations.map((e, i) => (
                <li key={i}>
                  {e.providerStatus} · {formatDateTime(e.processedAt)}
                </li>
              ))}
            </ul>
          </>
        )}
        <RefundHistory
          payment={payment}
          busy={busy}
          reconcile={(id) =>
            run(() => paymentsApi.reconcileRefund(id), "refund", id)
          }
        />
        <ConfirmDialog
          open={Boolean(intent)}
          onClose={() => setIntent(null)}
          title={intent?.title || "Confirm action"}
          pending={pending}
          onConfirm={() => run(intent.action, intent.kind)}
        >
          {intent?.detail}
        </ConfirmDialog>
      </div>
    </section>
  );
}
