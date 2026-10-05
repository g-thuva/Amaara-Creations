import { Link } from 'react-router-dom';
import { useResource } from '../hooks/useResource';
import { orderApi } from '../services/orderApi';
import { date } from '../utils/storefront';
import { EmptyState, ErrorState, PageHeader, Price, Skeleton } from '../components/storefront/UI';
export default function Orders() {
  const data = useResource(orderApi.getUserOrders, 'orders');
  return <><PageHeader title="Your orders">A record of your Amaara creations.</PageHeader>{data.loading ? <Skeleton count={2}/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : !data.data?.length ? <EmptyState title="Your first favourite is waiting.">You have not placed an order yet.</EmptyState> : <div className="s-orders">{data.data.map(order => <article className="s-panel" key={order.id}><div className="s-row"><h2><Link to={`/orders/${order.id}`}>{order.orderNumber || `Order ${order.id}`}</Link></h2><span className="s-badge">{order.status}</span></div><p>{date(order.orderDate)} · {order.orderItems.reduce((sum, item) => sum + item.quantity, 0)} items</p><div className="s-row"><Price value={order.total}/><Link className="s-text-link" to={`/orders/${order.id}`}>View order →</Link></div></article>)}</div>}</>;
}
