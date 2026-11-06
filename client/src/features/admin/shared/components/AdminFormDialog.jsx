import { useEffect, useId, useState } from 'react';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import './adminFormDialog.css';

// Mount for one editing session. Forms keep their own fields and API operations.
export function AdminFormDialog({ title, description, onClose, children, dirty, pending = false, submitDisabled = false, error, submitLabel = 'Save changes', submitVariant = 'primary', width = 'medium' }) {
  const formId = useId();
  const [changed, setChanged] = useState(false);
  const [confirmDiscard, setConfirmDiscard] = useState(false);
  const hasChanges = dirty ?? changed;
  const requestClose = () => {
    if (pending) return;
    if (hasChanges) setConfirmDiscard(true);
    else onClose();
  };
  useEffect(() => {
    if (!hasChanges) return undefined;
    const protect = event => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', protect);
    return () => window.removeEventListener('beforeunload', protect);
  }, [hasChanges]);

  return <>
    <Dialog open title={title} description={description} onClose={requestClose}
      className={`admin-form-dialog admin-form-dialog--${width}`} dismissOnBackdrop closeDisabled={pending}
      initialFocus="[data-dialog-autofocus]"
      footer={<footer className="admin-form-dialog-footer">
        <span className="admin-form-dialog-note">{pending ? 'Saving your changes…' : 'Required fields are marked with *.'}</span>
        <div className="admin-form-dialog-actions">
          <button type="button" className="btn btn-outline-primary" onClick={requestClose} disabled={pending}>Cancel</button>
          {submitLabel && <button type="submit" className={`btn btn-${submitVariant}`} form={formId} disabled={pending || submitDisabled} aria-busy={pending || undefined}>
            {pending && <span className="admin-spinner" aria-hidden="true" />}{pending ? 'Saving…' : submitLabel}
          </button>}
        </div>
      </footer>}>
      <div className="admin-form-dialog-body"
        onDragOver={event => { if (event.dataTransfer.types.includes('Files')) event.preventDefault(); }}
        onDropCapture={event => { if (event.dataTransfer.types.includes('Files')) event.preventDefault(); }}
        onChange={() => { if (dirty === undefined) setChanged(true); }}>
        {error && <p className="admin-form-error" role="alert">{error}</p>}
        {children(formId)}
      </div>
    </Dialog>
    <Dialog open={confirmDiscard} title="Discard changes?" description="Your unsaved changes and selected uploads will be lost." onClose={() => setConfirmDiscard(false)}
      className="admin-discard-dialog" initialFocus="[data-dialog-autofocus]" dismissOnBackdrop>
      <div className="admin-form-dialog-actions">
        <button type="button" className="btn btn-outline-primary" data-dialog-autofocus onClick={() => setConfirmDiscard(false)}>Continue Editing</button>
        <button type="button" className="btn btn-danger" onClick={onClose}>Discard Changes</button>
      </div>
    </Dialog>
  </>;
}
