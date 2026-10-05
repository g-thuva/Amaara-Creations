import { useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { resolveMediaUrl } from '../../services/config';
import { customerError, money, validQuantity } from '../../utils/storefront';

export function Icon({ name, ...props }) {
  const paths = {
    bag: <><path d="M5 7h14l1 14H4L5 7Z"/><path d="M9 8V6a3 3 0 0 1 6 0v2"/></>,
    heart: <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z"/>,
    user: <><circle cx="12" cy="8" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></>,
    menu: <path d="M4 6h16M4 12h16M4 18h16"/>,
    arrow: <path d="M4 12h16m-6-6 6 6-6 6"/>,
    search: <><circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/></>,
    image: <><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8" cy="8" r="1"/><path d="m3 17 5-5 4 4 4-7 5 8"/></>,
  };
  return <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" {...props}>{paths[name]}</svg>;
}
export function Button({ children, variant = 'primary', className = '', ...props }) {
  return <button className={`s-button s-button-${variant} ${className}`} {...props}>{children}</button>;
}
export function PageHeader({ eyebrow, title, children }) {
  return <header className="s-page-header">{eyebrow && <p className="s-eyebrow">{eyebrow}</p>}<h1>{title}</h1>{children && <div className="s-lead">{children}</div>}</header>;
}
export function SectionHeading({ title, to, label = 'Explore all' }) {
  return <div className="s-section-heading"><h2>{title}</h2>{to && <Link to={to}>{label} <span aria-hidden="true">↗</span></Link>}</div>;
}
export function Price({ value }) { return <span className="s-price">{money(value)}</span>; }
export function Media({ src, alt, eager = false, className = '' }) {
  const [failed, setFailed] = useState(null);
  const url = resolveMediaUrl(src);
  return <div className={`s-media ${className}`}>{url && failed !== url
    ? <img src={url} alt={alt} loading={eager ? 'eager' : 'lazy'} decoding="async" onError={() => setFailed(url)} />
    : <div className="s-image-fallback" role="img" aria-label={`${alt || 'Product'} — image unavailable`}><Icon name="image"/><span>Image coming soon</span></div>}</div>;
}
export function Skeleton({ count = 4 }) {
  return <div role="status" aria-label="Loading content" className="s-skeleton-grid">{Array.from({ length: count }, (_, i) => <div key={i} className="s-skeleton" aria-hidden="true" />)}</div>;
}
export function EmptyState({ title, children, to = '/products', label = 'Explore products' }) {
  return <div className="s-empty"><span className="s-empty-symbol" aria-hidden="true">✳</span><h2>{title}</h2>{children && <p>{children}</p>}{to && <Link className="s-button s-button-primary" to={to}>{label}<Icon name="arrow"/></Link>}</div>;
}
export function ErrorState({ error, retry }) {
  return <div className="s-alert s-error" role="alert"><p>{customerError(error)}</p>{retry && <Button variant="secondary" onClick={retry}>Try again</Button>}</div>;
}
export function Quantity({ value, onChange, max, disabled = false, label = 'Quantity' }) {
  const id = useId();
  return <div className="s-quantity"><label htmlFor={id} className="sr-only">{label}</label>
    <button type="button" disabled={disabled || value <= 1} aria-label={`Decrease ${label.toLowerCase()}`} onClick={() => onChange(value - 1)}>−</button>
    <input id={id} type="number" inputMode="numeric" min="1" max={max} step="1" value={value} disabled={disabled} onChange={e => { const next = Number(e.target.value); if (validQuantity(next, max)) onChange(next); }} />
    <button type="button" disabled={disabled || value >= max} aria-label={`Increase ${label.toLowerCase()}`} onClick={() => onChange(value + 1)}>+</button>
  </div>;
}
export function Dialog({ open, onClose, title, children, drawer = false }) {
  const ref = useRef(null);
  const titleId = useId();
  const closeRef = useRef(onClose);
  closeRef.current = onClose;
  useEffect(() => {
    if (!open) return;
    const dialog = ref.current;
    const previous = document.activeElement;
    const overflow = document.body.style.overflow;
    dialog.showModal();
    document.body.style.overflow = 'hidden';
    return () => { dialog.close(); document.body.style.overflow = overflow; previous?.focus(); };
  }, [open]);
  const trapFocus = event => {
    if (event.key !== 'Tab') return;
    const controls = [...ref.current.querySelectorAll('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex="0"]')].filter(element => element.getClientRects().length);
    const first = controls[0], last = controls.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
    if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  };
  return <dialog ref={ref} className={`s-dialog ${drawer ? 's-drawer' : ''}`} aria-labelledby={titleId} onKeyDown={trapFocus} onCancel={e => { e.preventDefault(); closeRef.current(); }}>
    <div className="s-dialog-head"><h2 id={titleId}>{title}</h2><Button variant="text" aria-label={`Close ${title.toLowerCase()}`} onClick={onClose}>×</Button></div>{open && children}
  </dialog>;
}
export function ConfirmDialog({ open, onClose, onConfirm, pending, title, children }) {
  return <Dialog open={open} onClose={pending ? () => {} : onClose} title={title}><p>{children}</p><div className="s-actions"><Button variant="secondary" disabled={pending} onClick={onClose} autoFocus>Cancel</Button><Button disabled={pending} onClick={onConfirm}>{pending ? 'Please wait…' : 'Confirm'}</Button></div></Dialog>;
}
export function Pagination({ page, totalPages, onChange }) {
  if (totalPages <= 1) return null;
  return <nav className="s-pagination" aria-label="Product pages"><Button variant="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>Previous</Button><span aria-live="polite">Page {page} of {totalPages}</span><Button variant="secondary" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>Next</Button></nav>;
}
