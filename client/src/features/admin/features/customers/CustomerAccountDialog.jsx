import { useState } from 'react';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { Button, Checkbox, Field, StatusChip, Textarea } from '@/features/admin/shared/components/AdminUi.jsx';
import { useMutation } from '@/shared/hooks/useMutation.js';
import { customerError } from '@/shared/api/apiError.ts';
import { customersApi } from './customersApi.js';
import { customerActions, availableCustomerActions } from './customerAccountActions.js';
import { useAuth } from '@/features/auth/AuthProvider.jsx';

export function CustomerAccountDialog({ customer, action, onClose, onSaved, stale = false }) {
  const { user } = useAuth();
  const [current, setCurrent] = useState(customer);
  const [reason, setReason] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);
  const mutation = useMutation();
  const reload = useMutation();
  const config = customerActions[action];
  const conflict = mutation.error?.response?.status === 409;
  const pending = mutation.pending || reload.pending;
  const allowed = availableCustomerActions(current, user, { includeReview: true }).includes(action);
  const save = async event => {
    event.preventDefault();
    if (stale || pending || conflict || !allowed || reason.trim().length < 10 || (config.acknowledgment && !acknowledged)) return;
    if (await mutation.run(() => customersApi.changeStatus(current.id, {
      status: action, reason: reason.trim(), version: current.version, acknowledged,
    }))) onSaved(config.success);
  };
  const refresh = () => reload.run(() => customersApi.getOperations(current.id), {
    onSuccess: data => { setCurrent(data.customer); mutation.reset(); },
  });

  return <AdminFormDialog title={config.label} description={`${current.name} · ${current.email}`}
    pending={pending} onClose={onClose} dirty={Boolean(reason || acknowledged)} width="compact"
    error={mutation.error ? customerError(mutation.error) : reload.error ? customerError(reload.error) : null}
    submitLabel={config.label} submitVariant={config.variant === 'secondary' ? 'primary' : config.variant}
    submitDisabled={stale || conflict || !allowed || !current.version || reason.trim().length < 10 || Boolean(config.acknowledgment && !acknowledged)}>
    {formId => <form id={formId} onSubmit={save} className="add-product-form">
      <p>Current account status: <StatusChip status={current.accountStatus} /></p>
      <p>{config.description}</p>
      {conflict && <Button variant="secondary" disabled={pending} onClick={refresh}>Reload current status</Button>}
      {!allowed && <p role="status">This action is no longer available for this account. Close this dialog and review the customer.</p>}
      <fieldset className="admin-form-fields" disabled={pending}>
        <Field label={<>Reason <span className="admin-required" aria-hidden="true" /></>}
          hint="Use 10–500 characters. Your name, the reason and the status change are recorded in account history.">
          <Textarea required data-dialog-autofocus minLength={10} maxLength={500} value={reason} onChange={event => setReason(event.target.value)} />
        </Field>
        {config.acknowledgment && <Checkbox label={config.acknowledgment} checked={acknowledged}
          onChange={event => setAcknowledged(event.target.checked)} required />}
      </fieldset>
    </form>}
  </AdminFormDialog>;
}
