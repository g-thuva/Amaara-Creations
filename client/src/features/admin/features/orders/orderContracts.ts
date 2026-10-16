import type { Order } from '@/features/orders/orderContracts';
export interface OrderFilters {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  status?: string;
}
export interface OrderList {
  orders: Array<
    Pick<
      Order,
      | "id"
      | "orderNumber"
      | "userName"
      | "createdAt"
      | "currency"
      | "total"
      | "status"
      | "paymentStatus"
      | "fulfilmentStatus"
    >
  >;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
export interface OrderTransition {
  status: string;
  reason: string;
}
