import { PaymentAttemptPanel } from "@/features/admin/features/payments/PaymentAttemptPanel.jsx";
// The order already owns payment summaries; only each attempt's detail is fetched.
export function OrderPayments({ payments, changed }) {
  return payments.map((payment) => (
    <PaymentAttemptPanel key={payment.id} summary={payment} changed={changed} />
  ));
}
