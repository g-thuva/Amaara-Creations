import { useResource } from "@/shared/hooks/useResource.js";
import { ordersApi } from "@/features/admin/features/orders/ordersApi.js";
export function useOrderDetail(id) {
  return useResource((signal) => ordersApi.getOrder(id, signal), id, true, {
    keepPreviousData: true,
  });
}
