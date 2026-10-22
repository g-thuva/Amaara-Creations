import { Link, useParams } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import { can } from "@/app/config/permissions.js";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { useOrderDetail } from "@/features/admin/features/orders/useOrderDetail.js";
import { OrderSummary } from "@/features/admin/features/orders/OrderSummary.jsx";
import { OrderItems } from "@/features/admin/features/orders/OrderItems.jsx";
import { OrderTimeline } from "@/features/admin/features/orders/OrderTimeline.jsx";
import { OrderTransitionForm } from "@/features/admin/features/orders/OrderTransitionForm.jsx";
import { OrderPayments } from "@/features/admin/features/payments/OrderPayments.jsx";
import { OrderShipments } from "@/features/admin/features/shipping/OrderShipments.jsx";
import { AdminPage } from "@/features/admin/shared/components/AdminPage.jsx";
import { PageHeader } from "@/features/admin/shared/components/PageHeader.jsx";
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
import { Skeleton } from '@/features/admin/shared/components/Skeleton.jsx';
import { ErrorState } from '@/features/admin/shared/components/ErrorState.jsx';
export default function OrderDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const resource = useOrderDetail(id);
  const order = resource.data;
  if (resource.error && !order)
    return (
      <ErrorState title="Order details could not be loaded." retry={resource.reload} />
    );
  if (!order) return <Skeleton label="Loading order…" />;
  const canManage = can(user, "ManageOrders");
  const stale = resource.loading || Boolean(resource.error);
  return (
    <AdminPage>
      <Link to="/admin/orders">All orders</Link>
      <PageHeader title={order.orderNumber} />
      <p>
        Order: <StatusChip status={order.status} /> · Payment: <StatusChip status={order.paymentStatus} /> · Fulfilment:{" "}
        <StatusChip status={order.fulfilmentStatus} />
      </p>
      <p>
        {order.userName} · {order.shippingMethodName}
      </p>
      <p>Total: {checkoutMoney(order.total, order.currency)}</p>
      <div className="admin-detail-grid"><div className="admin-detail-main">
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
      </div><aside className="admin-detail-rail" aria-label="Order summary and history"><OrderSummary order={order} /><OrderTimeline order={order} /></aside></div>
    </AdminPage>
  );
}
