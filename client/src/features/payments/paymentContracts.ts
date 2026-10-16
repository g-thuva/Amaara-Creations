export interface Refund {
  id: number;
  amount: number;
  currency: string;
  status: "Pending" | "Completed" | "Failed";
  reason: string | null;
  createdAt: string;
  providerTransactionId: string | null;
}
export interface PaymentEvent {
  providerStatus: string;
  processedAt: string;
}
export interface Payment {
  id: number;
  orderId: number;
  orderStatus: string;
  redirectUrl: string | null;
  expiresAt: string | null;
  provider: string;
  status: string;
  currency: string;
  amount: number;
  authorizedAmount: number;
  settledAmount: number;
  refundedAmount: number;
  refundableAmount: number;
  providerAvailable: boolean;
  providerMode: string | null;
  providerStatus: string | null;
  providerGatewayId: string | null;
  providerReference: string | null;
  providerTransactionId: string | null;
  attemptNumber: number;
  createdAt: string;
  paidAt: string | null;
  lastReconciledAt: string | null;
  unableToVerify: boolean;
  refunds: Refund[];
  events: PaymentEvent[];
  reconciliations: PaymentEvent[];
}

export interface PaymentSession {
  orderId: number;
  paymentId: number;
  status: string;
  redirectUrl: string | null;
  expiresAt: string | null;
}
