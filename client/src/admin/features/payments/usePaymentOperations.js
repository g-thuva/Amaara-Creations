import { useRef, useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { paymentsApi } from "./paymentsApi.js";
import { checkoutMoney } from "../../../services/checkoutApi.js";
import { refundNotice } from "./refundNotice.js";

export function usePaymentOperations(paymentId, changed) {
  const resource = useResource(
    (signal) => paymentsApi.getPayment(paymentId, signal),
    String(paymentId),
    true,
    { keepPreviousData: true },
  );
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState(null);
  const [intent, setIntent] = useState(null);
  const refundRequest = useRef(null);
  const operationKeys = useRef({});
  const payment = resource.data;
  const locked = useRef(false);
  const busy =
    pending ||
    resource.loading ||
    Boolean(resource.error) ||
    payment?.providerAvailable === false;
  const run = async (action, kind = "payment", refundId) => {
    if (busy || locked.current) return;
    locked.current = true;
    setPending(true);
    setNotice(null);
    try {
      let result = await action();
      if (kind === "refund") {
        // Reconciliation returns 204. Read the actual refund instead of treating
        // the HTTP status or an empty response as a completed credit.
        if (!result?.status)
          result = (await paymentsApi.getPayment(paymentId)).refunds.find(
            (r) => r.id === refundId,
          );
        if (["Completed", "Failed"].includes(result?.status))
          refundRequest.current = null;
        setNotice(refundNotice(result));
      } else
        setNotice({
          text: result?.status
            ? "Payment status: " + result.status + "."
            : "Payment status refreshed.",
        });
      setIntent(null);
    } catch (e) {
      setIntent(null);
      setNotice({
        error: true,
        text:
          e.response?.status < 500
            ? e.response?.data?.title ||
              "The payment action could not be completed."
            : "The payment action could not be verified.",
        detail:
          "Refresh payment details and review refund history before trying again.",
      });
    } finally {
      resource.reload();
      changed();
      locked.current = false;
      setPending(false);
    }
  };
  const prepareRefund = (event) => {
    event.preventDefault();
    if (busy) return;
    const form = Object.fromEntries(new FormData(event.currentTarget));
    const identity = JSON.stringify([form.amount, form.reason]);
    if (refundRequest.current?.identity !== identity)
      refundRequest.current = {
        identity,
        request: {
          amount: Number(form.amount),
          reason: form.reason,
          idempotencyKey: crypto.randomUUID(),
        },
      };
    const request = refundRequest.current.request;
    setIntent({
      title: "Confirm refund",
      kind: "refund",
      detail:
        "Refund " +
        checkoutMoney(Number(form.amount), payment.currency) +
        ": " +
        form.reason,
      action: () => paymentsApi.refundPayment(payment.id, request),
    });
  };
  const refresh = () => {
    resource.reload();
    changed();
  };
  const prepareAuthorization = (action) => {
    const key = (operationKeys.current[action] ||= crypto.randomUUID());
    setIntent({
      title:
        action === "settle"
          ? "Confirm settlement"
          : "Confirm authorization reversal",
      detail:
        action === "settle"
          ? "Capture the authorized order total."
          : "Release the authorization without charging the customer.",
      action: () =>
        action === "settle"
          ? paymentsApi.settlePayment(payment.id, key)
          : paymentsApi.reverseAuthorization(payment.id, key),
    });
  };
  return {
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
  };
}
