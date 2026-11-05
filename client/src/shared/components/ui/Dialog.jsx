import { useEffect, useId, useRef } from "react";
import AppIcon from "@/shared/components/ui/Icon.jsx";
import { lockDialogScroll } from './dialogScrollLock.js';
export function Dialog({ open, onClose, title, description, children, drawer = false, id, className = '', footer, dismissOnBackdrop = false, closeDisabled = false, initialFocus }) {
  const ref = useRef(null);
  const titleId = useId();
  const descriptionId = useId();
  const closeRef = useRef(onClose);
  closeRef.current = onClose;
  const backdropPointer = useRef(false);

  useEffect(() => {
    if (!open) return undefined;
    const dialog = ref.current;
    const previous = document.activeElement;
    const unlock = lockDialogScroll();
    if (!dialog.open) dialog.showModal();
    if (initialFocus) dialog.querySelector(initialFocus)?.focus({ preventScroll: true });
    return () => {
      dialog.close();
      unlock();
      queueMicrotask(() => { if (previous?.isConnected) previous.focus({ preventScroll: true }); });
    };
  }, [open, initialFocus]);

  const outside = event => {
    const rect = ref.current.getBoundingClientRect();
    return event.target === ref.current && (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom);
  };

  const trapFocus = (event) => {
    if (event.key !== "Tab") return;
    const controls = [
      ...ref.current.querySelectorAll(
        'button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex="0"]',
      ),
    ].filter((element) => element.getClientRects().length && element.tabIndex >= 0);
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

  const handleBackdropClick = (event) => {
    if (event.target !== ref.current) return;
    if (event.clientX === 0 && event.clientY === 0) return;
    const rect = ref.current.getBoundingClientRect();
    const clickedInside =
      event.clientX >= rect.left &&
      event.clientX <= rect.right &&
      event.clientY >= rect.top &&
      event.clientY <= rect.bottom;
    if (!clickedInside) {
      closeRef.current?.();
    }
  };

  return (
    <dialog
      id={id}
      ref={ref}
      className={`s-dialog ${drawer ? "s-drawer" : ""} ${className}`.trim()}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onKeyDown={trapFocus}
      onClick={handleBackdropClick}
      onCancel={(event) => {
        event.preventDefault();
        if (!closeDisabled) closeRef.current();
      }}
      onPointerDown={event => { backdropPointer.current = outside(event); }}
      onPointerCancel={() => { backdropPointer.current = false; }}
      onDragOver={event => { if (dismissOnBackdrop && event.target === ref.current && event.dataTransfer.types.includes('Files')) event.preventDefault(); }}
      onDrop={event => {
        if (dismissOnBackdrop && event.target === ref.current && event.dataTransfer.types.includes('Files')) event.preventDefault();
        backdropPointer.current = false;
      }}
      onClick={event => {
        if (dismissOnBackdrop && !closeDisabled && backdropPointer.current && outside(event)) closeRef.current();
        backdropPointer.current = false;
      }}
    >
      <div className="s-dialog-head">
        <h2 id={titleId}>{title}</h2>
        <button
          type="button"
          className="s-button s-button-icon"
          aria-label={`Close ${title.toLowerCase()}`}
          onClick={onClose}
          disabled={closeDisabled}
        >
          <AppIcon name="x" />
        </button>
      </div>
      {description && <p id={descriptionId} className="s-dialog-description">{description}</p>}
      {open && children}
      {open && footer}
    </dialog>
  );
}
