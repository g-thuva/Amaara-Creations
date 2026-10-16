import { useResource } from "../../../hooks/useResource.js";
import { ordersApi } from "./ordersApi.js";
export function useOrderDetail(id) {
  return useResource((signal) => ordersApi.getOrder(id, signal), id, true, {
    keepPreviousData: true,
  });
}
