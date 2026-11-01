import { useEffect, useId, useRef } from "react";
import AppIcon from "@/shared/components/ui/Icon.jsx";
export function Dialog({ open, onClose, title, description, children, drawer = false, id }) {
  const ref = useRef(null);
  const titleId = useId();
  const descriptionId = useId();
  const closeRef = useRef(onClose);
  closeRef.current = onClose;

  useEffect(() => {
    if (!open) return undefined;
    const dialog = ref.current;
    const previous = document.activeElement;
    const overflow = document.body.style.overflow;
    if (!dialog.open) dialog.showModal();
    document.body.style.overflow = "hidden";
    return () => {
      dialog.close();
      document.body.style.overflow = overflow;
      previous?.focus();
    };
  }, [open]);

  const trapFocus = (event) => {
    if (event.key !== "Tab") return;
    const controls = [
      ...ref.current.querySelectorAll(
        'button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex="0"]',
      ),
    ].filter((element) => element.getClientRects().length);
    const first = controls[0];
    const last = controls.at(-1);
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last?.focus();
    }
    if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first?.focus();
    }
  };

  return (
    <dialog
      id={id}
      ref={ref}
      className={`s-dialog ${drawer ? "s-drawer" : ""}`.trim()}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onKeyDown={trapFocus}
      onCancel={(event) => {
        event.preventDefault();
        closeRef.current();
      }}
    >
      <div className="s-dialog-head">
        <h2 id={titleId}>{title}</h2>
        <button
          className="s-button s-button-icon"
          aria-label={`Close ${title.toLowerCase()}`}
          onClick={onClose}
        >
          <AppIcon name="x" />
        </button>
      </div>
      {description && <p id={descriptionId} className="s-dialog-description">{description}</p>}
      {open && children}
    </dialog>
  );
}
