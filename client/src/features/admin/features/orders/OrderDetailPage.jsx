import { Link, useParams } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import { can } from "@/app/config/permissions.js";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
import { useOrderDetail } from "@/features/admin/features/orders/useOrderDetail.js";
import { OrderSummary } from "@/features/admin/features/orders/OrderSummary.jsx";
import { OrderItems } from "@/features/admin/features/orders/OrderItems.jsx";
import { OrderTimeline } from "@/features/admin/features/orders/OrderTimeline.jsx";
import { OrderTransitionForm } from "@/features/admin/features/orders/OrderTransitionForm.jsx";
import { OrderPayments } from "@/features/admin/features/payments/OrderPayments.jsx";
import { OrderShipments } from "@/features/admin/features/shipping/OrderShipments.jsx";
import { AdminPage } from "@/features/admin/shared/components/AdminPage.jsx";
import { PageHeader } from "@/features/admin/shared/components/PageHeader.jsx";
export default function OrderDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const resource = useOrderDetail(id);
  const order = resource.data;
  if (resource.error && !order)
    return (
      <p role="alert">
        Order details could not be loaded.{" "}
        <button onClick={resource.reload}>Refresh order details</button>
      </p>
    );
  if (!order) return <p role="status">Loading order…</p>;
  const canManage = can(user, "ManageOrders");
  const stale = resource.loading || Boolean(resource.error);
  return (
    <AdminPage>
      <Link to="/admin/orders">All orders</Link>
      <PageHeader title={order.orderNumber} />
      <p>
        Order: {order.status} · Payment: {order.paymentStatus} · Fulfilment:{" "}
        {order.fulfilmentStatus}
      </p>
      <p>
        {order.userName} · {order.shippingMethodName}
      </p>
      <p>Total: {checkoutMoney(order.total, order.currency)}</p>
      <OrderSummary order={order} />
      <OrderItems order={order} canManageOrders={canManage} />
      {can(user, "ManagePayments") && (
        <OrderPayments payments={order.payments} changed={resource.reload} />
      )}
      {resource.loading && <p role="status">Refreshing order details…</p>}
      {resource.error && (
        <p role="alert">
          Order details could not be refreshed.{" "}
          <button onClick={resource.reload}>Refresh order details</button>
        </p>
      )}
      {canManage && order.availableTransitions?.length > 0 && (
        <OrderTransitionForm
          order={order}
          changed={resource.reload}
          stale={stale}
        />
      )}
      <OrderShipments
        order={order}
        changed={resource.reload}
        canManage={canManage}
        stale={stale}
      />
      <OrderTimeline order={order} />
    </AdminPage>
  );
}
