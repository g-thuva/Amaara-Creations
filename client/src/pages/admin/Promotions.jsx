import { useState } from 'react';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { DataTable, ResourceState, Pager, useMutation } from './OperationsUI';
import { checkoutMoney } from '../../services/checkoutApi';

const blank = { code: '', name: '', discountType: 1, discountValue: '', currency: '', startsAt: '', endsAt: '', minimumSpend: '', maximumDiscount: '', usageLimit: '', perCustomerUsageLimit: '', isActive: false, appliesToStandard: true, appliesToCustom: true };
export default function Promotions() {
  const [page, setPage] = useState(1); const [editing, setEditing] = useState(null); const [historyId, setHistoryId] = useState(null);
  const resource = useResource(signal => api.get('promotions', { page }, signal), String(page));
  const history = useResource(signal => api.get(`promotions/${historyId}/redemptions`, null, signal), String(historyId), Boolean(historyId));
  const mutation = useMutation(resource.reload);
  const submit = async event => {
    event.preventDefault(); const values = Object.fromEntries(new FormData(event.currentTarget));
    const nullable = key => values[key] === '' ? null : Number(values[key]);
    const request = { ...values, discountValue: Number(values.discountValue), minimumSpend: nullable('minimumSpend'), maximumDiscount: nullable('maximumDiscount'), usageLimit: nullable('usageLimit'), perCustomerUsageLimit: nullable('perCustomerUsageLimit'),
      startsAt: new Date(values.startsAt).toISOString(), endsAt: values.endsAt ? new Date(values.endsAt).toISOString() : null,
      isActive: values.isActive === 'on', appliesToStandard: values.appliesToStandard === 'on', appliesToCustom: values.appliesToCustom === 'on', rowVersion: editing.rowVersion };
    const ok = await mutation.run(() => editing.id ? api.put(`promotions/${editing.id}`, request) : api.post('promotions', request));
    if (ok) setEditing(null);
  };
  const localDate = value => value ? new Date(new Date(value).getTime() - new Date(value).getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '';
  return <div className="admin-page"><h1>Promotions</h1><button className="btn btn-primary" onClick={() => setEditing(blank)}>Create promotion</button>{mutation.feedback}
    {editing && <form key={editing.id || 'new'} className="card card-body add-product-form" onSubmit={submit}><h2>{editing.id ? 'Edit promotion' : 'New promotion'}</h2><p>Dates use your local time and are stored in UTC. Checkout validates every redemption.</p>
      <div className="form-row"><label>Code<input name="code" className="form-control" maxLength="40" pattern="[A-Za-z0-9_-]+" defaultValue={editing.code} required /></label><label>Name<input className="form-control" name="name" maxLength="160" defaultValue={editing.name} required /></label><label>Currency<select className="form-control" name="currency" defaultValue={editing.currency} required><option value="">Choose currency</option>{['LKR', 'CHF', 'USD', 'EUR', 'GBP'].map(c => <option key={c}>{c}</option>)}</select></label></div>
      <div className="form-row"><label>Discount type<select className="form-control" name="discountType" defaultValue={Number(editing.discountType) === 0 ? 'Percentage' : 'FixedAmount'}><option>FixedAmount</option><option>Percentage</option></select></label><label>Discount value<input className="form-control" name="discountValue" type="number" min="0.01" step="0.01" defaultValue={editing.discountValue} required /></label><label>Starts<input className="form-control" name="startsAt" type="datetime-local" defaultValue={localDate(editing.startsAt)} required /></label><label>Ends (optional)<input className="form-control" name="endsAt" type="datetime-local" defaultValue={localDate(editing.endsAt)} /></label></div>
      <div className="form-row">{[['minimumSpend', 'Minimum order'], ['maximumDiscount', 'Maximum discount'], ['usageLimit', 'Usage limit'], ['perCustomerUsageLimit', 'Per-customer limit']].map(([key, label]) => <label key={key}>{label} (optional)<input className="form-control" name={key} type="number" min={key.includes('Limit') ? '1' : '0'} step={key.includes('Limit') ? '1' : '0.01'} defaultValue={editing[key] ?? ''} /></label>)}</div>
      {[['isActive', 'Active'], ['appliesToStandard', 'Standard items'], ['appliesToCustom', 'Custom items']].map(([key, label]) => <label key={key}><input type="checkbox" name={key} defaultChecked={editing[key]} /> {label}</label>)}
      <button className="btn btn-primary" disabled={mutation.pending}>Save promotion</button><button type="button" className="btn btn-outline-primary" onClick={() => setEditing(null)}>Cancel</button>
    </form>}
    <ResourceState resource={resource}><DataTable caption="Promotion rules" rows={resource.data?.items || []} columns={[{ key: 'code', label: 'Code' }, { key: 'name', label: 'Name' }, { key: 'discountValue', label: 'Discount', render: r => r.discountType === 0 ? `${r.discountValue}%` : checkoutMoney(r.discountValue, r.currency) }, { key: 'isActive', label: 'State', render: r => r.isActive ? 'Active' : 'Disabled' }, { key: 'usageCount', label: 'Committed uses' }, { key: 'usageLimit', label: 'Limit' }, { key: 'actions', label: 'Actions', render: r => <><button className="btn btn-outline-primary" onClick={() => setEditing(r)}>Edit {r.code}</button><button className="btn btn-outline-primary" onClick={() => setHistoryId(r.id)}>History {r.code}</button></> }]} /><Pager page={page} setPage={setPage} size={30} total={resource.data?.totalCount || 0} /></ResourceState>
    {historyId && <ResourceState resource={history}><DataTable caption="Promotion redemption history (latest 50)" rows={history.data?.items || []} columns={[{ key: 'orderId', label: 'Order' }, { key: 'status', label: 'State' }, { key: 'amount', label: 'Discount', render: r => checkoutMoney(r.amount, r.currency) }, { key: 'createdAt', label: 'Time', render: r => new Date(r.createdAt).toLocaleString() }]} /></ResourceState>}
  </div>;
}
