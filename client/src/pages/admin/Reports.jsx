import { useState } from 'react';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { checkoutMoney } from '../../services/checkoutApi';
import { DataTable, ResourceState, useMutation } from './OperationsUI';

export default function Reports() {
  const [dates, setDates] = useState({});
  const resource = useResource(signal => api.get('reports', dates, signal), JSON.stringify(dates));
  const mutation = useMutation();
  const exportOrders = () => mutation.run(async () => { const blob = await api.exportOrders(dates); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = 'orders.csv'; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000); }, 'Order export downloaded.');
  return <div className="admin-page"><h1>Reports</h1><p>Financial values remain separate by currency and payment environment. Order totals include unpaid orders; settled funds are shown separately.</p>
    <form className="form-row" onSubmit={e => { e.preventDefault(); const f = new FormData(e.currentTarget); setDates({ from: f.get('from') || undefined, to: f.get('to') || undefined }); }}><label>From (UTC)<input className="form-control" type="date" name="from" /></label><label>Before (UTC, exclusive)<input className="form-control" type="date" name="to" /></label><button className="btn btn-primary">Apply dates</button><button type="button" className="btn btn-outline-primary" disabled={mutation.pending || resource.loading || Boolean(resource.error)} onClick={exportOrders}>Export orders CSV</button></form>{mutation.feedback}
    <ResourceState resource={resource}>{resource.data && <>
      <DataTable caption="Settled funds by currency and environment" rows={resource.data.money} columns={[{ key: 'currency', label: 'Currency' }, { key: 'providerMode', label: 'Environment', render: r => r.providerMode || 'Historical / unspecified' }, { key: 'attempts', label: 'Attempts' }, { key: 'settled', label: 'Settled', render: r => checkoutMoney(r.settled, r.currency) }, { key: 'refunded', label: 'Refunded', render: r => checkoutMoney(r.refunded, r.currency) }]} />
      <DataTable caption="Orders by status" rows={resource.data.orders} columns={[{ key: 'status', label: 'State' }, { key: 'currency', label: 'Currency' }, { key: 'count', label: 'Count' }, { key: 'total', label: 'Order total', render: r => checkoutMoney(r.total, r.currency) }]} />
      <DataTable caption="Items on confirmed paid orders" rows={resource.data.products} columns={[{ key: 'product', label: 'Snapshot product' }, { key: 'type', label: 'Type' }, { key: 'currency', label: 'Currency' }, { key: 'quantity', label: 'Quantity' }, { key: 'total', label: 'Line total', render: r => checkoutMoney(r.total, r.currency) }]} />
      <DataTable caption="Current production backlog" rows={resource.data.production} columns={[{ key: 'state', label: 'State' }, { key: 'count', label: 'Jobs' }]} />
      <DataTable caption="Inventory movements" rows={resource.data.inventory} columns={[{ key: 'type', label: 'Type' }, { key: 'count', label: 'Movements' }, { key: 'delta', label: 'Available-stock delta' }]} />
      <DataTable caption="Promotion usage" rows={resource.data.promotions} columns={[{ key: 'code', label: 'Code' }, { key: 'currency', label: 'Currency' }, { key: 'status', label: 'State' }, { key: 'count', label: 'Redemptions' }, { key: 'discount', label: 'Discount', render: r => checkoutMoney(r.discount, r.currency) }]} />
    </>}</ResourceState>
  </div>;
}
