import { Dialog } from '@/shared/components/ui/Dialog.jsx';
export function ConfirmDialog({ open, onClose, onConfirm, pending, title, children, destructive = false }) {
  return <Dialog open={open} onClose={pending ? () => {} : onClose} title={title}><div className="admin-confirm-body">{children}</div><div className="s-actions"><button className="btn btn-outline-primary" disabled={pending} onClick={onClose} autoFocus>Cancel</button><button className={`btn btn-${destructive ? 'danger' : 'primary'}`} disabled={pending} aria-busy={pending || undefined} onClick={onConfirm}>{pending && <span className="admin-spinner" aria-hidden="true" />}{pending ? 'Please wait…' : 'Confirm'}</button></div></Dialog>;
}
