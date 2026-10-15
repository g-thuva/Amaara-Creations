import { Link, useParams } from 'react-router-dom';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { checkoutMoney } from '../../services/checkoutApi';
import { DataTable, ResourceState } from './OperationsUI';

export default function CustomerDetail() {
  const { id } = useParams(); const resource = useResource(signal => api.get(`customers/${id}/operations`, null, signal), id);
  const data = resource.data;
  return <div className="admin-page"><Link to="/admin/customers">All customers</Link><h1>Customer details</h1><ResourceState resource={resource}>{data && <>
    <section className="card card-body"><h2>{data.customer.name}</h2><p>{data.customer.email} · {data.customer.phone}</p><p>Email: {data.customer.emailConfirmed ? 'Verified' : 'Unverified'} · Account: {data.customer.locked ? 'Locked' : 'Active'}</p><p>Joined: {new Date(data.customer.createdAt).toLocaleDateString()} · {data.orderCount} orders</p>
    <h3>Lifetime settled order value</h3>{data.lifetimeValue.length ? data.lifetimeValue.map(v => <p key={`${v.currency}-${v.providerMode}`}>{checkoutMoney(v.settled - v.refunded, v.currency)} · {v.providerMode || 'Historical mode unspecified'} · Refunds {checkoutMoney(v.refunded, v.currency)}</p>) : <p>No settled payments.</p>}</section>
    <h2>Saved addresses</h2>{data.addresses.length ? data.addresses.map(a => <address className="card card-body" key={a.id}><strong>{a.label}{a.isDefault && ' · Default'}</strong><p>{a.recipientName}<br />{a.addressLine1} {a.addressLine2}<br />{a.city} {a.districtOrProvince} {a.postalCode}<br />{a.country}<br />{a.phone}</p></address>) : <p>No saved addresses.</p>}
    <DataTable caption="Recent customer orders (up to 50)" rows={data.orders} columns={[{ key: 'orderNumber', label: 'Order', render: r => <Link to={`/admin/orders/${r.id}`}>{r.orderNumber}</Link> }, { key: 'orderDate', label: 'Date', render: r => new Date(r.orderDate).toLocaleDateString() }, { key: 'status', label: 'Status' }, { key: 'total', label: 'Total', render: r => checkoutMoney(r.total, r.currency) }]} />
    <DataTable caption="Recent custom designs (up to 50)" rows={data.designs} columns={[{ key: 'name', label: 'Name' }, { key: 'status', label: 'Design' }, { key: 'proofStatus', label: 'Proof' }, { key: 'productionStatus', label: 'Production' }]} />
    <DataTable caption="Recent customer reviews (up to 50)" rows={data.reviews} columns={[{ key: 'productId', label: 'Product' }, { key: 'rating', label: 'Rating' }, { key: 'comment', label: 'Review' }, { key: 'moderationStatus', label: 'Moderation' }]} />
  </>}</ResourceState></div>;
}
