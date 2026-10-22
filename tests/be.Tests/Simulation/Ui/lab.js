'use strict';
const byId = id => document.getElementById(id);
let snapshot;
let lastEvent;
let lastRefund;
let working = false;
async function api(path, data) {
  const response = await fetch('/simulator/' + path, { credentials: 'same-origin', ...(data === undefined ? {} : { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }) });
  const result = await response.json();
  if (!response.ok) throw new Error(result.message || result.detail || 'The local simulator rejected this request.');
  return result;
}
async function action(operation) {
  if (working) return; working = true;
  byId('error').hidden = true; document.querySelectorAll('button').forEach(button => { button.disabled = true; });
  try { await operation(); }
  catch (error) { byId('error').textContent = error.message; byId('error').hidden = false; }
  finally { working = false; document.querySelectorAll('button').forEach(button => { button.disabled = false; }); byId('replay').disabled = !lastEvent; byId('refund-replay').disabled = !lastRefund; }
}
function selected() {
  const payment = snapshot.payments.find(value => String(value.id) === byId('payment').value);
  if (!payment) throw new Error('Create a checkout in the test shop first.');
  return payment;
}
function summary() {
  if (!snapshot.payments.length) return;
  const payment = selected();
  byId('summary').textContent = `SIMULATED ${payment.currency} ${payment.amount.toFixed(2)} · Payment #${payment.id}: ${payment.status} · Order #${payment.orderId}: ${payment.orderStatus}`;
  byId('return').href = `http://127.0.0.1:5188/Amaara-Creations/checkout/payment/return?paymentId=${payment.id}`;
}
async function refresh() {
  snapshot = await api('api/inspect');
  byId('database').textContent = `Disposable SQL: ${snapshot.database} · ${snapshot.currency} · LocalSimulator / SIMULATED`;
  const previous = byId('payment').value;
  byId('payment').replaceChildren();
  if (!snapshot.payments.length) byId('payment').append(new Option('Create a checkout in the shop first', ''));
  for (const payment of snapshot.payments) byId('payment').append(new Option(`#${payment.id} · order ${payment.orderId} · ${payment.currency} ${payment.amount.toFixed(2)} · ${payment.status}`, String(payment.id)));
  const hostedId = location.pathname.match(/^\/simulator\/pay\/([0-9]+)$/)?.[1];
  const hosted = snapshot.payments.find(value => value.providerTransactionId === hostedId);
  if (snapshot.payments.some(value => String(value.id) === previous)) byId('payment').value = previous;
  else if (hosted) byId('payment').value = String(hosted.id);
  byId('payments').replaceChildren();
  for (const payment of snapshot.payments) {
    const row = document.createElement('tr');
    for (const text of [payment.id, payment.orderId, `${payment.currency} ${payment.amount.toFixed(2)}`, payment.status, payment.orderStatus, payment.refundedAmount.toFixed(2), `${payment.provider} / ${payment.providerMode}`]) { const cell = document.createElement('td'); cell.textContent = text; row.append(cell); }
    byId('payments').append(row);
  }
  byId('snapshot').textContent = JSON.stringify(snapshot, null, 2); summary();
}
document.querySelectorAll('[data-session]').forEach(button => button.addEventListener('click', () => action(async () => { const result = await api('session/' + button.dataset.session, {}); location.assign(result.url); })));
document.querySelectorAll('[data-outcome]').forEach(button => button.addEventListener('click', () => action(async () => {
  lastEvent = { paymentId: selected().id, outcome: button.dataset.outcome, eventId: crypto.randomUUID(), delivery: byId('delivery').value };
  await api('api/events', lastEvent); await refresh(); byId('notice').textContent = lastEvent.delivery === 'webhook' ? 'Signed simulated event processed by the backend. Results below are read from SQL.' : 'Simulated provider state recorded. Use Reconcile to apply it; no webhook was delivered.';
})));
byId('payment').addEventListener('change', summary);
byId('refresh').addEventListener('click', () => action(refresh));
byId('replay').addEventListener('click', () => action(async () => { await api('api/events', lastEvent); await refresh(); byId('notice').textContent = 'The same event was replayed. The backend deduplicates its effects.'; }));
byId('reconcile').addEventListener('click', () => action(async () => { await api(`api/payments/${selected().id}/reconcile`, {}); await refresh(); byId('notice').textContent = 'Normal backend reconciliation completed.'; }));
byId('refund').addEventListener('submit', event => { event.preventDefault(); action(async () => {
  lastRefund = { paymentId: selected().id, amount: Number(byId('refund-amount').value), outcome: byId('refund-outcome').value, idempotencyKey: crypto.randomUUID() };
  const result = await api(`api/payments/${lastRefund.paymentId}/refund`, lastRefund); await refresh(); byId('notice').textContent = `SIMULATED refund #${result.id}: ${result.status}.`;
}); });
byId('refund-replay').addEventListener('click', () => action(async () => { await api(`api/payments/${lastRefund.paymentId}/refund`, lastRefund); await refresh(); byId('notice').textContent = 'The same refund request was replayed. No additional refund should be created.'; }));
byId('refund-complete').addEventListener('click', () => action(async () => { const refund = snapshot.refunds.find(value => value.paymentId === selected().id && value.status === 'Pending'); if (!refund) throw new Error('The selected payment has no pending refund.'); await api(`api/refunds/${refund.id}/complete`, {}); await refresh(); byId('notice').textContent = 'Pending simulated refund completed through the signed-event workflow.'; }));
action(refresh);
