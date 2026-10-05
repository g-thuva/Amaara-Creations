import { Link, useLocation, useParams } from 'react-router-dom';
import { useResource } from '../hooks/useResource';
import { orderApi } from '../services/orderApi';
import { date } from '../utils/storefront';
import { EmptyState, ErrorState, Media, PageHeader, Price, Skeleton } from '../components/storefront/UI';
export default function OrderDetails() {
  const { id } = useParams(); const location = useLocation();
  const data = useResource(() => orderApi.getOrderById(id), id); const order = data.data;
  return <><Link to="/orders" className="s-text-link">← All orders</Link>{data.loading ? <Skeleton count={2}/> : data.error?.response?.status === 404 ? <EmptyState title="Order not found" to="/orders" label="Back to orders"/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : <><PageHeader title={order.orderNumber || `Order ${order.id}`}>{date(order.orderDate)}</PageHeader>{location.state?.message && <p className="s-alert" role="status">{location.state.message}</p>}<p><span className="s-badge">{order.status}</span></p><div className="s-cart-items">{order.orderItems.map(item => <article className="s-cart-item" key={item.id}><Media src={item.productImageUrl} alt={item.productName}/><div><h2>{item.productName}</h2><p>{item.quantity} × <Price value={item.price}/></p></div><Price value={item.subtotal}/></article>)}</div><div className="s-row s-order-total"><strong>Order total</strong><Price value={order.total}/></div>{order.shippingAddress && <section className="s-panel"><h2>Delivery address</h2><address>{order.shippingAddress}<br/>{order.shippingCity} {order.shippingPostalCode}<br/>{order.shippingCountry}</address></section>}</>}</>;
}
