import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { can } from '@/app/config/permissions.js';
import { Button } from '@/features/admin/shared/components/AdminUi.jsx';
import { MutationFeedback } from '@/features/admin/shared/components/MutationFeedback.jsx';
import { CustomerAccountDialog } from './CustomerAccountDialog.jsx';
import { availableCustomerActions, customerActions } from './customerAccountActions.js';

export function CustomerAccountActions({ customer, changed, stale }) {
  const { user } = useAuth();
  const [action, setAction] = useState(null);
  const [success, setSuccess] = useState(null);
  const permitted = can(user, 'ManageCustomerAccess');
  const owner = can(user, 'RequestCustomerRemoval');
  const actions = availableCustomerActions(customer, user, { includeReview: true });
  if (!permitted) return null;
  return <section className="card card-body">
    <h2>Account access</h2>
    <p>Changes revoke existing sessions. Orders, payments, reviews and audit records are retained.</p>
    <MutationFeedback success={success} />
    <div className="action-buttons">{actions.map(status => <Button key={status} variant={customerActions[status].variant} disabled={stale} onClick={() => { setSuccess(null); setAction(status); }}>{customerActions[status].label}</Button>)}</div>
    {!actions.length && <p>Customer access actions are unavailable for this account. Staff accounts and removal reviews have separate management rules.</p>}
    {owner && <p className="admin-helper"><Link to="/admin/privacy-requests">Review account-removal requests</Link></p>}
    {action && <CustomerAccountDialog key={action} customer={customer} action={action} stale={stale} onClose={() => setAction(null)}
      onSaved={message => { setAction(null); setSuccess(message); changed?.(); }} />}
  </section>;
}
