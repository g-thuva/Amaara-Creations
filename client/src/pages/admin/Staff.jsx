import { useState } from 'react';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { DataTable, ResourceState, useMutation } from './OperationsUI';
import { ConfirmDialog } from '../../components/storefront/UI';
export default function Staff() {
  const resource = useResource(signal => api.get('staff', null, signal)); const [intent, setIntent] = useState(null); const mutation = useMutation(resource.reload);
  const submit = e => { e.preventDefault(); const form = new FormData(e.currentTarget); setIntent({ email: form.get('email'), reason: form.get('reason'), roles: form.getAll('roles') }); };
  return <div className="admin-page"><h1>Staff permissions</h1><p>Assign access to an existing verified account. Replacing roles revokes refresh sessions. A second Super Admin must change your own access.</p>{mutation.feedback}
    <ResourceState resource={resource}><DataTable caption="Authorized staff" rows={resource.data?.items || []} columns={[{ key: 'name', label: 'Name' }, { key: 'email', label: 'Email' }, { key: 'roles', label: 'Roles', render: r => r.roles.join(', ') }]} />
      <form className="card card-body add-product-form" onSubmit={submit}><h2>Replace staff role assignment</h2><label>Verified account email<input className="form-control" type="email" name="email" required /></label><fieldset><legend>New roles (leave all clear to remove staff access)</legend>{resource.data?.roles.map(role => <label className="s-check" key={role}><input type="checkbox" name="roles" value={role} /> {role}</label>)}</fieldset><label>Reason<textarea className="form-control" name="reason" maxLength="500" required /></label><button className="btn btn-primary" disabled={mutation.pending}>Review staff change</button></form>
    </ResourceState><ConfirmDialog open={Boolean(intent)} onClose={() => setIntent(null)} title="Confirm staff permissions" pending={mutation.pending} onConfirm={async () => { await mutation.run(() => api.put('staff', intent)); setIntent(null); }}>Assign {intent?.roles.join(', ') || 'no staff roles'} to {intent?.email}. Reason: {intent?.reason}</ConfirmDialog>
  </div>;
}
