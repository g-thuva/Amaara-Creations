import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../../contexts/AuthContext.jsx";
import { can } from "../../app/adminPermissions.js";
import { checkoutMoney } from "../../../services/checkoutApi.js";
import { useOrderDetail } from "./useOrderDetail.js";
import { OrderSummary } from "./OrderSummary.jsx";
import { OrderItems } from "./OrderItems.jsx";
import { OrderTimeline } from "./OrderTimeline.jsx";
import { OrderTransitionForm } from "./OrderTransitionForm.jsx";
import { OrderPayments } from "../payments/OrderPayments.jsx";
import { OrderShipments } from "../shipping/OrderShipments.jsx";
import { AdminPage } from "../../shared/components/AdminPage.jsx";
import { PageHeader } from "../../shared/components/PageHeader.jsx";
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
