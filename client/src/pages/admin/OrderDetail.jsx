import { useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useResource } from '../../hooks/useResource';
import { orderApi } from '../../services/orderApi';
import { checkoutAdminApi } from '../../services/checkoutAdminApi';
import { checkoutMoney } from '../../services/checkoutApi';
import { ConfirmDialog } from '../../components/storefront/UI';
import { useAuth } from '../../contexts/AuthContext';
import { can } from '../../utils/permissions';
import { operationsApi } from '../../services/operationsApi';
import { refundNotice } from '../../services/refundNotice';

function PaymentPanel({ summary, changed }) {
  const resource = useResource(signal => checkoutAdminApi.payment(summary.id, signal), String(summary.id), true, { keepPreviousData: true });
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState(null);
  const [intent, setIntent] = useState(null);
  const refundRequest = useRef(null);
  const operationKeys = useRef({});
  const payment = resource.data;
  const busy = pending || resource.loading || Boolean(resource.error) || payment?.providerAvailable === false;
  const run = async (action, kind = 'payment', refundId) => {
    if (busy) return; setPending(true); setNotice(null);
    try {
      let result = await action();
      if (kind === 'refund') {
        // Reconciliation returns 204. Read the actual refund instead of treating
        // the HTTP status or an empty response as a completed credit.
        if (!result?.status) result = (await checkoutAdminApi.payment(summary.id)).refunds.find(r => r.id === refundId);
        if (['Completed', 'Failed'].includes(result?.status)) refundRequest.current = null;
        setNotice(refundNotice(result));
      } else setNotice({ text: result?.status ? 'Payment status: ' + result.status + '.' : 'Payment status refreshed.' });
      setIntent(null);
    }
    catch (e) {
      setIntent(null);
      setNotice({ error: true, text: e.response?.status < 500 ? e.response?.data?.title || 'The payment action could not be completed.' : 'The payment action could not be verified.', detail: 'Refresh payment details and review refund history before trying again.' });
    }
    finally { resource.reload(); changed(); setPending(false); }
  };
  const prepareRefund = event => {
    event.preventDefault(); if (busy) return; const form = Object.fromEntries(new FormData(event.currentTarget));
    const identity = JSON.stringify([form.amount, form.reason]);
    if (refundRequest.current?.identity !== identity) refundRequest.current = { identity, request: { amount: Number(form.amount), reason: form.reason, idempotencyKey: crypto.randomUUID() } };
    setIntent({ title: 'Confirm refund', kind: 'refund', detail: 'Refund ' + checkoutMoney(Number(form.amount), payment.currency) + ': ' + form.reason, action: () => checkoutAdminApi.refund(payment.id, refundRequest.current.request) });
  };
  if (resource.error && !payment) return <p role="alert">Payment details could not be loaded. <button onClick={resource.reload}>Refresh payment details</button></p>;
  if (!payment) return <p role="status">Loading payment…</p>;
  return <section className="card"><div className="card-header"><h3>Payment attempt {payment.attemptNumber || payment.id}</h3></div><div className="card-body">
    <p>{payment.provider}: {payment.status} · {checkoutMoney(payment.amount, payment.currency)}</p><p>Reference: {payment.providerReference}</p><p>Transaction: {payment.providerTransactionId || 'Not initialized'}</p><p>Settled: {checkoutMoney(payment.settledAmount, payment.currency)} · Refunded: {checkoutMoney(payment.refundedAmount, payment.currency)}</p>
    {payment.createdAt && <p>Created: {new Date(payment.createdAt).toLocaleString()}</p>}{payment.paidAt && <p>Confirmed: {new Date(payment.paidAt).toLocaleString()}</p>}
    {payment.providerAvailable === false && <p role="status">Provider operations are unavailable until its private configuration and acceptance checks are complete.</p>}
    {payment.providerMode && <p>Mode: {payment.providerMode}</p>}
    {payment.providerGatewayId && <p>Gateway ID: {payment.providerGatewayId}</p>}
    {payment.providerStatus && <p>Provider state: {payment.providerStatus}</p>}
    {payment.lastReconciledAt && <p>Last checked: {new Date(payment.lastReconciledAt).toLocaleString()}</p>}
    {payment.events?.length > 0 && <><h4>Provider event history</h4><ul>{payment.events.map((event, index) => <li key={`${event.processedAt}-${index}`}>{event.providerStatus} — {new Date(event.processedAt).toLocaleString()}</li>)}</ul></>}
    {notice && <><p role={notice.error ? 'alert' : 'status'}>{notice.text}</p>{notice.detail && <p>{notice.detail}</p>}</>}
    {resource.loading && <p role="status">Refreshing payment details…</p>}
    {resource.error && <p role="alert">Payment details could not be refreshed. Refresh before submitting another refund.</p>}
    <p>Remaining refundable: {checkoutMoney(payment.refundableAmount, payment.currency)}</p>
    <div className="action-buttons"><button className="btn btn-outline-primary" disabled={pending || resource.loading} onClick={() => { resource.reload(); changed(); }}>Refresh payment details</button><button className="btn btn-outline-primary" disabled={busy} onClick={() => run(() => checkoutAdminApi.reconcile(payment.id))}>Reconcile payment</button>{payment.status === 'Authorized' && ['settle', 'cancel'].map(action => <button className="btn btn-primary" key={action} disabled={busy} onClick={() => { const key = operationKeys.current[action] ||= crypto.randomUUID(); setIntent({ title: action === 'settle' ? 'Confirm settlement' : 'Confirm authorization reversal', detail: action === 'settle' ? 'Capture the authorized order total.' : 'Release the authorization without charging the customer.', action: () => checkoutAdminApi[action](payment.id, key) }); }}>{action === 'settle' ? 'Settle authorized payment' : 'Reverse authorization'}</button>)}</div>
    {payment.refundableAmount > 0 && ['Paid', 'PartiallyRefunded'].includes(payment.status) && <form className="add-product-form" onSubmit={prepareRefund}><h4>Refund settled funds</h4><p>Available: {checkoutMoney(payment.refundableAmount, payment.currency)}. Physical stock is managed separately.</p><div className="form-row"><label>Refund amount<input className="form-control" name="amount" type="number" step="0.01" min="0.01" max={payment.refundableAmount} required disabled={busy} /></label><label>Refund reason<textarea className="form-control" name="reason" maxLength="500" required disabled={busy} /></label></div><button className="btn btn-primary" disabled={busy}>Review refund</button></form>}
    {payment.reconciliations?.length > 0 && <><h4>Reconciliation history</h4><ul>{payment.reconciliations.map((e, i) => <li key={i}>{e.providerStatus} · {new Date(e.processedAt).toLocaleString()}</li>)}</ul></>}
    <h4>Refund history</h4>{payment.refunds.length ? <ul>{payment.refunds.map(r => <li key={r.id}>{checkoutMoney(r.amount, r.currency)} — {r.status}: {r.reason}{r.status === 'Pending' && <button className="btn btn-outline-primary" disabled={busy} onClick={() => run(() => checkoutAdminApi.reconcileRefund(r.id), 'refund', r.id)}>Reconcile refund</button>}</li>)}</ul> : <p>No refunds.</p>}
    <ConfirmDialog open={Boolean(intent)} onClose={() => setIntent(null)} title={intent?.title || 'Confirm action'} pending={pending} onConfirm={() => run(intent.action, intent.kind)}>{intent?.detail}</ConfirmDialog>
  </div></section>;
}

export default function OrderDetail() {
  const { id } = useParams();
  const { user } = useAuth();
  const resource = useResource(signal => orderApi.getAdminOrderById(id, signal), id, true, { keepPreviousData: true });
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState(null);
  const [transition, setTransition] = useState(null);
  const order = resource.data;
  const updateShipment = async event => {
    event.preventDefault(); if (pending) return;
    const form = Object.fromEntries(new FormData(event.currentTarget));
    setPending(true); setNotice(null);
    try { await checkoutAdminApi.shipment(id, { ...form, courier: form.courier || null, trackingNumber: form.trackingNumber || null, trackingUrl: form.trackingUrl || null }); resource.reload(); setNotice({ success: true, text: 'Fulfilment updated.' }); }
    catch (e) { setNotice({ text: e.response?.data?.title || 'Fulfilment could not be updated.' }); }
    finally { setPending(false); }
  };
  if (resource.error && !order) return <p role="alert">Order details could not be loaded. <button onClick={resource.reload}>Refresh order details</button></p>;
  if (!order) return <p role="status">Loading order…</p>;
  const shipment = order.shipments?.[0];
  const shipmentState = shipment?.status || 'Pending';
  const nextShipment = order.isPickup ? { Pending: 'ReadyForPickup', ReadyForPickup: 'Delivered' }[shipmentState] : { Pending: 'Packed', Packed: 'Shipped', Shipped: 'Delivered' }[shipmentState];
  return <div className="admin-page"><Link to="/admin/orders">All orders</Link><div className="page-header"><h1>{order.orderNumber}</h1></div><p>Order: {order.status} · Payment: {order.paymentStatus} · Fulfilment: {order.fulfilmentStatus}</p><p>{order.userName} · {order.shippingMethodName}</p><p>Total: {checkoutMoney(order.total, order.currency)}</p>
    <section className="card card-body"><h2>Order snapshot</h2><p>Created: {new Date(order.createdAt).toLocaleString()}</p>
      <p>Customer: {order.userName} · {order.userEmail}</p><p>Recipient: {order.shippingRecipientName || 'Not recorded'} · {order.shippingPhone || 'No phone recorded'}</p>
      <address>{order.shippingAddress}<br />{order.shippingCity} {order.shippingRegion} {order.shippingPostalCode}<br />{order.shippingCountry}</address>
      <p>Shipping: {order.shippingMethodName} · {checkoutMoney(order.shippingTotal, order.currency)}</p><p>Subtotal: {checkoutMoney(order.subtotal, order.currency)} · Discount: {checkoutMoney(order.discountTotal, order.currency)} {order.couponCode}</p>
      <p>Tax: {checkoutMoney(order.taxTotal, order.currency)} · Payment fee: {checkoutMoney(order.paymentFee, order.currency)}</p><p>Order notes: {order.notes || 'None'}</p>
    </section>
    <section className="card card-body"><h2>Item snapshots</h2>{order.orderItems.map(i => <article key={i.id}><h3>{i.productName}</h3><p>{i.itemType} · SKU: {i.sku || 'Not recorded'} · Quantity: {i.quantity}</p><p>Unit: {checkoutMoney(i.price, order.currency)} · Line: {checkoutMoney(i.subtotal, order.currency)}</p>
      {i.customDesignId && can(user, 'ManageOrders') && <Link to={`/admin/custom-designs/${i.customDesignId}`}>Production and private artwork</Link>}
      {i.configurationSnapshotJson && <details><summary>Immutable custom production specification</summary><pre>{JSON.stringify(JSON.parse(i.configurationSnapshotJson), null, 2)}</pre></details>}
    </article>)}</section>
    {order.shipments?.map(s => <section className="card card-body" key={s.id}><h2>Shipment details</h2><p>{s.status} · {s.courier} · Tracking {s.trackingNumber || "Not assigned"}</p><p>Dispatched: {s.shippedAt ? new Date(s.shippedAt).toLocaleString() : "Not dispatched"} · Delivered: {s.deliveredAt ? new Date(s.deliveredAt).toLocaleString() : "Not delivered"}</p></section>)}
    {can(user, 'ManagePayments') && order.payments.map(p => <PaymentPanel key={p.id} summary={p} changed={resource.reload} />)}
    {resource.loading && <p role="status">Refreshing order details…</p>}
    {resource.error && <p role="alert">Order details could not be refreshed. <button onClick={resource.reload}>Refresh order details</button></p>}
    {notice && <p role={notice.success ? 'status' : 'alert'}>{notice.text}</p>}
    {can(user, 'ManageOrders') && order.availableTransitions?.length > 0 && <section className="card card-body"><h2>Order transition</h2><form className="add-product-form" onSubmit={e => { e.preventDefault(); setTransition(Object.fromEntries(new FormData(e.currentTarget))); }}><label>Next order state<select className="form-control" name="status">{order.availableTransitions.map(s => <option key={s}>{s}</option>)}</select></label><label>Transition reason<textarea className="form-control" name="reason" maxLength="500" required /></label><button className="btn btn-primary" disabled={pending}>Review order transition</button></form></section>}
    {can(user, 'ManageOrders') && (nextShipment || shipment) && ['Confirmed', 'Processing', 'Shipped', 'Completed', 'Delivered'].includes(order.status) && <section className="card"><div className="card-header"><h3>Shipment or pickup</h3></div><form className="card-body add-product-form" key={shipmentState} onSubmit={updateShipment}><label>Next fulfilment status<select className="form-control" name="status">{nextShipment && <option value={nextShipment}>{nextShipment}</option>}{shipment && <option value={shipmentState}>Correct tracking ({shipmentState})</option>}</select></label><div className="form-row"><label>Carrier<input className="form-control" name="courier" defaultValue={shipment?.courier || ""} maxLength="120" /></label><label>Tracking number<input className="form-control" name="trackingNumber" defaultValue={shipment?.trackingNumber || ""} maxLength="120" /></label><label>Tracking URL<input className="form-control" name="trackingUrl" defaultValue={shipment?.trackingUrl || ""} type="url" pattern="https://.*" maxLength="500" /></label></div><label>Shipment notes<textarea className="form-control" name="notes" maxLength="500" /></label><button className="btn btn-primary" disabled={pending}>Update fulfilment</button></form></section>}
    <section className="card"><div className="card-header"><h3>Order history</h3></div><ul>{order.history.map((h, i) => <li key={i}><time>{new Date(h.createdAt).toLocaleString()}</time> · {h.previousStatus} → {h.newStatus}: {h.reason}</li>)}</ul></section>
    <ConfirmDialog open={Boolean(transition)} onClose={() => setTransition(null)} title="Confirm order transition" pending={pending} onConfirm={async () => { if (pending) return; setPending(true); try { await operationsApi.put(`orders/${id}/status`, transition); setNotice({ success: true, text: 'Order transition recorded.' }); resource.reload(); } catch (e) { setNotice({ text: e.response?.data?.title || 'Order transition could not be saved.' }); } finally { setTransition(null); setPending(false); } }}>{transition?.status}: {transition?.reason}</ConfirmDialog>
  </div>;
}
