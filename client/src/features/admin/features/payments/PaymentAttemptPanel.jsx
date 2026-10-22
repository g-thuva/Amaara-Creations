import { formatDateTime } from "@/features/admin/shared/utils/format.js";
import { usePaymentOperations } from "@/features/admin/features/payments/usePaymentOperations.js";
import { paymentsApi } from "@/features/admin/features/payments/paymentsApi.js";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { ConfirmDialog } from "@/features/admin/shared/components/ConfirmDialog.jsx";
import { PaymentActions } from "@/features/admin/features/payments/PaymentActions.jsx";
import { RefundForm } from "@/features/admin/features/payments/RefundForm.jsx";
import { RefundHistory } from "@/features/admin/features/payments/RefundHistory.jsx";
import { StatusChip, CopyableId } from '@/features/admin/shared/components/AdminUi.jsx';
import { Skeleton } from '@/features/admin/shared/components/Skeleton.jsx';
import { ErrorState } from '@/features/admin/shared/components/ErrorState.jsx';
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
      <ErrorState title="Payment details could not be loaded." retry={resource.reload} />
    );
  if (!payment) return <Skeleton label="Loading payment…" />;
  return (
    <section className="card">
      <div className="card-header">
        <h2>Payment attempt {payment.attemptNumber || payment.id}</h2>
      </div>
      <div className="card-body">
        <p>
          {payment.provider}: <StatusChip status={payment.status} /> ·{" "}
          {checkoutMoney(payment.amount, payment.currency)}
        </p>
        <p>Reference: <CopyableId value={payment.providerReference} label="payment reference" /></p>
        <p>Transaction: {payment.providerTransactionId ? <CopyableId value={payment.providerTransactionId} label="transaction identifier" /> : 'Not initialized'}</p>
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
          <p>Gateway ID: <CopyableId value={payment.providerGatewayId} label="gateway identifier" /></p>
        )}
        {payment.providerStatus && (
          <p>Provider state: {payment.providerStatus}</p>
        )}
        {payment.lastReconciledAt && (
          <p>Last checked: {formatDateTime(payment.lastReconciledAt)}</p>
        )}
        {payment.events?.length > 0 && (
          <>
            <h3>Provider event history</h3>
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
            <h3>Reconciliation history</h3>
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
