import { useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import AppIcon from '../AppIcon';
import { resolveMediaUrl } from '../../services/config';
import { customerError, money, validQuantity } from '../../utils/storefront';

export const Icon = AppIcon;

export function Button({ children, variant = 'primary', className = '', ...props }) {
  return (
    <button className={`s-button s-button-${variant} ${className}`.trim()} {...props}>
      {children}
    </button>
  );
}

export function PageHeader({ eyebrow, title, children, className = '' }) {
  return (
    <header className={`s-page-header ${className}`.trim()}>
      {eyebrow && <p className="s-eyebrow">{eyebrow}</p>}
      <h1>{title}</h1>
      <span className="s-title-accent" aria-hidden="true" />
      {children && <div className="s-lead">{children}</div>}
    </header>
  );
}

export function SectionHeading({ title, to, label = 'Explore all', align = 'center' }) {
  return (
    <div className={`s-section-heading s-section-heading-${align}`}>
      <div>
        <h2>{title}</h2>
        <span className="s-title-accent" aria-hidden="true" />
      </div>
      {to && (
        <Link to={to}>
          {label}
          <Icon name="arrow" />
        </Link>
      )}
    </div>
  );
}

export function Price({ value }) {
  return <span className="s-price">{money(value)}</span>;
}

export function Media({ src, alt, eager = false, className = '', width = 800, height = 600, objectPosition }) {
  const [failed, setFailed] = useState(null);
  const url = resolveMediaUrl(src);

  return (
    <div className={`s-media ${className}`.trim()}>
      {url && failed !== url ? (
        <img
          src={url}
          alt={alt || ''}
          width={width}
          height={height}
          loading={eager ? 'eager' : 'lazy'}
          fetchPriority={eager ? 'high' : 'auto'}
          decoding="async"
          style={objectPosition ? { objectPosition } : undefined}
          onError={() => setFailed(url)}
        />
      ) : (
        <div className="s-image-fallback" role="img" aria-label={`${alt || 'Product'} — image unavailable`}>
          <span className="s-sticker-glyph" aria-hidden="true"><Icon name="image" /></span>
          <span>Image coming soon</span>
        </div>
      )}
    </div>
  );
}

export function Skeleton({ count = 4 }) {
  return (
    <div role="status" aria-label="Loading content" className="s-skeleton-grid">
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="s-skeleton" aria-hidden="true" />
      ))}
    </div>
  );
}

export function EmptyState({ title, children, to = '/products', label = 'Explore products' }) {
  return (
    <div className="s-empty">
      <span className="s-empty-symbol" aria-hidden="true"><Icon name="package" /></span>
      <h2>{title}</h2>
      {children && <div className="s-empty-copy">{children}</div>}
      {to && (
        <Link className="s-button s-button-primary" to={to}>
          {label}<Icon name="arrow" />
        </Link>
      )}
    </div>
  );
}

export function ErrorState({ error, retry }) {
  return (
    <div className="s-alert s-error" role="alert">
      <p>{customerError(error)}</p>
      {retry && <Button variant="secondary" onClick={retry}>Try again</Button>}
    </div>
  );
}

export function Quantity({ value, onChange, max, disabled = false, label = 'Quantity' }) {
  const id = useId();
  return (
    <div className="s-quantity">
      <label htmlFor={id} className="sr-only">{label}</label>
      <button type="button" disabled={disabled || value <= 1} aria-label={`Decrease ${label.toLowerCase()}`} onClick={() => onChange(value - 1)}>
        <Icon name="minus" />
      </button>
      <input
        id={id}
        type="number"
        inputMode="numeric"
        min="1"
        max={max}
        step="1"
        value={value}
        disabled={disabled}
        onChange={(event) => {
          const next = Number(event.target.value);
          if (validQuantity(next, max)) onChange(next);
        }}
      />
      <button type="button" disabled={disabled || value >= max} aria-label={`Increase ${label.toLowerCase()}`} onClick={() => onChange(value + 1)}>
        <Icon name="plus" />
      </button>
    </div>
  );
}

export function Dialog({ open, onClose, title, children, drawer = false }) {
  const ref = useRef(null);
  const titleId = useId();
  const closeRef = useRef(onClose);
  closeRef.current = onClose;

  useEffect(() => {
    if (!open) return undefined;
    const dialog = ref.current;
    const previous = document.activeElement;
    const overflow = document.body.style.overflow;
    dialog.showModal();
    document.body.style.overflow = 'hidden';
    return () => {
      dialog.close();
      document.body.style.overflow = overflow;
      previous?.focus();
    };
  }, [open]);

  const trapFocus = (event) => {
    if (event.key !== 'Tab') return;
    const controls = [...ref.current.querySelectorAll('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex="0"]')]
      .filter((element) => element.getClientRects().length);
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
      ref={ref}
      className={`s-dialog ${drawer ? 's-drawer' : ''}`.trim()}
      aria-labelledby={titleId}
      onKeyDown={trapFocus}
      onCancel={(event) => {
        event.preventDefault();
        closeRef.current();
      }}
    >
      <div className="s-dialog-head">
        <h2 id={titleId}>{title}</h2>
        <Button variant="icon" aria-label={`Close ${title.toLowerCase()}`} onClick={onClose}>
          <Icon name="x" />
        </Button>
      </div>
      {open && children}
    </dialog>
  );
}

export function ConfirmDialog({ open, onClose, onConfirm, pending, title, children }) {
  return (
    <Dialog open={open} onClose={pending ? () => {} : onClose} title={title}>
      <p>{children}</p>
      <div className="s-actions">
        <Button variant="secondary" disabled={pending} onClick={onClose} autoFocus>Cancel</Button>
        <Button disabled={pending} onClick={onConfirm}>{pending ? 'Please wait…' : 'Confirm'}</Button>
      </div>
    </Dialog>
  );
}

export function Pagination({ page, totalPages, onChange }) {
  if (totalPages <= 1) return null;
  return (
    <nav className="s-pagination" aria-label="Product pages">
      <Button variant="secondary" disabled={page <= 1} aria-label="Previous product page" onClick={() => onChange(page - 1)}>
        <Icon name="chevronLeft" />Previous
      </Button>
      <span aria-current="page" aria-live="polite">Page {page} of {totalPages}</span>
      <Button variant="secondary" disabled={page >= totalPages} aria-label="Next product page" onClick={() => onChange(page + 1)}>
        Next<Icon name="chevronRight" />
      </Button>
    </nav>
  );
}
