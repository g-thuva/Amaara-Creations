import { cloneElement, useState } from 'react';
import { ConfirmDialog } from './ConfirmDialog.jsx';
// Adds a review step around an existing callback; owns no request or payload.
export function ConfirmAction({ children, onConfirm, title, detail, pending, destructive = true }) {
  const [open, setOpen] = useState(false); const [confirming, setConfirming] = useState(false);
  async function confirm() { if (confirming) return; setConfirming(true); try { await onConfirm(); setOpen(false); } finally { setConfirming(false); } }
  return <>{cloneElement(children, { type: 'button', onClick: () => setOpen(true) })}<ConfirmDialog open={open} onClose={() => setOpen(false)} onConfirm={confirm} pending={pending || confirming} title={title} destructive={destructive}>{detail}</ConfirmDialog></>;
}
