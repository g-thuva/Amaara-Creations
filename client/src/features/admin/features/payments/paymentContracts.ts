import type { Payment } from '@/features/payments/paymentContracts';
// Response fields used by admin operations, matching PaymentStatusDto/RefundStatusDto.
export interface RefundRequest {
  amount: number;
  reason: string;
  idempotencyKey: string;
}
export interface PaymentFilters {
  page?: number;
  status?: string;
  mode?: string;
}
export interface PaymentList {
  items: Array<
    Pick<
      Payment,
      | "id"
      | "orderId"
      | "provider"
      | "providerMode"
      | "status"
      | "currency"
      | "amount"
      | "createdAt"
      | "paidAt"
      | "providerGatewayId"
      | "providerReference"
    > & { orderNumber: string }
  >;
  totalCount: number;
}
