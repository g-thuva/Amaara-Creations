import { Dialog } from "@/shared/components/ui/Dialog.jsx";
export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  pending,
  title,
  children,
}) {
  return (
    <Dialog open={open} onClose={pending ? () => {} : onClose} title={title}>
      <p>{children}</p>
      <div className="s-actions">
        <button
          className="s-button s-button-secondary"
          disabled={pending}
          onClick={onClose}
          autoFocus
        >
          Cancel
        </button>
        <button
          className="s-button s-button-primary"
          disabled={pending}
          onClick={onConfirm}
        >
          {pending ? "Please wait…" : "Confirm"}
        </button>
      </div>
    </Dialog>
  );
}
