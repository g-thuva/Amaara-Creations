import { useState } from 'react';
import { Checkbox, Callout, Field, Input, Textarea } from '@/features/admin/shared/components/AdminUi.jsx';
const descriptions = { Admin: 'Manage daily store, order and payment operations.', SuperAdmin: 'Manage sensitive settings and staff access.', CatalogManager: 'Manage products, collections and catalog stock.', ProductionStaff: 'Manage orders, production and inventory.', CustomerSupport: 'Help customers and review orders and reviews.', Marketing: 'Manage content, promotions and reviews.', Analyst: 'View reports and store performance.' };
export function StaffRoleForm({ resource, submit, mutation }) {
  const [selected, setSelected] = useState([]);
  return (
    <form className="card card-body add-product-form" onSubmit={submit}>
      <h2>Replace staff role assignment</h2>
      <Field label="Verified account email" hint="Use the email of an existing, verified account."><Input type="email" name="email" required /></Field>
      <fieldset>
        <legend>New roles (leave all clear to remove staff access)</legend>
        <div className="admin-role-grid">{resource.data?.roles.map(role => <Checkbox key={role} label={role} description={descriptions[role] || 'Access defined by the existing staff permission policy.'} name="roles" value={role} checked={selected.includes(role)} onChange={event => setSelected(current => event.target.checked ? [...current, role] : current.filter(value => value !== role))} />)}</div>
      </fieldset>
      {!selected.length && <Callout tone="danger"><p>Leave all clear to remove staff access. Review this carefully before confirming.</p></Callout>}
      <Field label="Reason" hint="Explain why this access is needed. The reason is kept in the audit record."><Textarea name="reason" maxLength="500" required /></Field>
      <button className="btn btn-primary" disabled={mutation.pending}>
        Review staff change
      </button>
    </form>
  );
}
