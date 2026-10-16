import { Link } from 'react-router-dom';
import { useResource } from '@/shared/hooks/useResource.js';
import { ordersApi } from '@/features/orders/ordersApi.js';
import { formatDate as date } from '@/shared/utils/formatDate.ts';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';

function statusIcon(status) {
  const value = status?.toLowerCase();
  if (value === 'delivered') return 'check';
  if (value === 'shipped') return 'truck';
  if (value === 'cancelled') return 'x';
  return 'package';
}
export default function OrdersPage() {
  const data = useResource(ordersApi.getUserOrders, 'orders');
  return (
    <>
      <PageHeader title="Your orders">Review orders recorded through your Amaara account.</PageHeader>
      {data.loading ? <Skeleton count={2} /> : data.error ? <ErrorState error={data.error} retry={data.reload} /> : !data.data?.length ? (
        <EmptyState to="/products" label="Explore products" title="No orders yet"><p>Available products can be added to your cart from the shop.</p></EmptyState>
      ) : (
        <div className="s-orders">
          {data.data.map((order) => (
            <article className="s-panel" key={order.id}>
              <div className="s-row">
                <h2><Link to={`/orders/${order.id}`}>{order.orderNumber || `Order ${order.id}`}</Link></h2>
                <span className="s-badge"><Icon name={statusIcon(order.status)} />{order.status}</span>
              </div>
              <p>{date(order.orderDate)} · {order.orderItems.reduce((sum, item) => sum + item.quantity, 0)} items</p>
              <p>Payment: {order.paymentStatus || 'Unpaid'} · Fulfilment: {order.fulfilmentStatus || 'NotReady'}</p>
              <div className="s-row"><Price value={order.total} currency={order.currency} /><Link className="s-text-link" to={`/orders/${order.id}`}>View order <Icon name="arrow" /></Link></div>
            </article>
          ))}
        </div>
      )}
    </>
  );
}
