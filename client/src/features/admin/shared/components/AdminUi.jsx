import { createContext, createElement, useContext, useId, useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
export { SectionCard as Card } from './SectionCard.jsx';
export { PageHeader } from './PageHeader.jsx';
export { DataTable } from './DataTable.jsx';
export { EmptyState } from './EmptyState.jsx';
export { ErrorState } from './ErrorState.jsx';
export { Skeleton } from './Skeleton.jsx';
export { StatusChip } from './StatusChip.jsx';
export { Pagination } from './Pagination.jsx';
export { FilterBar } from './FilterBar.jsx';
export { RowActions } from './RowActions.jsx';
export { ConfirmDialog } from './ConfirmDialog.jsx';
const FieldContext = createContext({});
export function Button({ variant = 'primary', size, loading, disabled, children, className = '', type = 'button', ...props }) {
  return <button {...props} type={type} disabled={disabled || loading} aria-busy={loading || undefined} className={`btn btn-${variant === 'secondary' ? 'outline-primary' : variant} ${size === 'small' ? 'btn-sm' : ''} ${className}`}>
    {loading && <span className="admin-spinner" aria-hidden="true" />}{children}
  </button>;
}
export function IconButton({ label, icon, ...props }) { return <Button {...props} variant={props.variant || 'ghost'} aria-label={label} title={label} className="admin-icon-button"><AppIcon name={icon} /></Button>; }
export function StatCard({ icon, value, label, description }) {
  return <article className="stat-card"><div className="stat-icon"><AppIcon name={icon} /></div><div className="stat-info"><span className="stat-value">{value}</span><span className="stat-title">{label}</span></div>{description && <p className="stat-change">{description}</p>}</article>;
}
export function Field({ label, hint, error, children, id: providedId }) {
  const generatedId = useId(); const id = providedId || generatedId;
  const describedBy = [hint && id + '-hint', error && id + '-error'].filter(Boolean).join(' ') || undefined;
  return <div className="admin-field"><label htmlFor={id}>{label}</label><FieldContext.Provider value={{ id, 'aria-describedby': describedBy, 'aria-invalid': error ? true : undefined }}>{children}</FieldContext.Provider>{hint && <small id={id + '-hint'}>{hint}</small>}{error && <p id={id + '-error'} className="admin-field-error"><AppIcon name="x" />{error}</p>}</div>;
}
function Control({ as: element = 'input', className = '', ...props }) { const field = useContext(FieldContext); return createElement(element, { ...field, ...props, className: `form-control ${className}` }); }
export function Input(props) { return <Control {...props} />; }
export function Select(props) { return <Control as="select" {...props} />; }
export function Textarea(props) { return <Control as="textarea" {...props} />; }
export function DateInput(props) { return <Input type="date" {...props} />; }
export function SearchInput(props) { return <Input type="search" {...props} />; }
export function Checkbox({ label, description, ...props }) { return <label className="admin-check"><input type="checkbox" {...props} /><span><strong>{label}</strong>{description && <small>{description}</small>}</span></label>; }
export function Switch(props) { return <Checkbox {...props} role="switch" />; }
export function Callout({ tone = 'info', children, title }) { return <div className={`admin-callout admin-callout-${tone}`}><AppIcon name={tone === 'danger' ? 'x' : tone === 'warning' ? 'pause' : 'file'} /><div>{title && <strong>{title}</strong>}{children}</div></div>; }
export function Disclosure({ title = 'View safe metadata', children }) { return <details className="admin-disclosure"><summary>{title}</summary><div>{children}</div></details>; }
export function Tooltip({ label, children }) { const id = useId(); const [dismissed, setDismissed] = useState(false); return <span className={`admin-tooltip ${dismissed ? 'is-dismissed' : ''}`} tabIndex={0} aria-describedby={id} onFocus={() => setDismissed(false)} onMouseEnter={() => setDismissed(false)} onKeyDown={event => { if (event.key === 'Escape') { event.stopPropagation(); setDismissed(true); } }}>{children}<span role="tooltip" id={id}>{label}</span></span>; }
export function CopyableId({ value, label = 'identifier' }) {
  const [feedback, setFeedback] = useState('');
  if (!value) return <span>—</span>;
  const full = String(value); const short = full.length > 20 ? full.slice(0, 8) + '…' + full.slice(-5) : full;
  const copy = async () => { try { await navigator.clipboard.writeText(full); setFeedback('Copied'); } catch { setFeedback('Copy unavailable. Select the full identifier from its tooltip.'); } };
  return <span className="admin-copy-id"><Tooltip label={full}><code>{short}</code></Tooltip><button type="button" className="admin-copy-button" onClick={copy} aria-label={`Copy ${label}`} title={full}><AppIcon name="layers" /></button><span className="admin-sr-only" role="status">{feedback}</span></span>;
}
export function Timeline({ children, label = 'Status history' }) { return <ol className="admin-timeline" aria-label={label}>{children}</ol>; }
export function Tabs({ items, value, onChange, label = 'Sections' }) {
  return <div className="admin-tabs" role="tablist" aria-label={label}>{items.map((item, index) => <button key={item.value} type="button" role="tab" aria-selected={value === item.value} tabIndex={value === item.value ? 0 : -1} aria-controls={item.panelId} onClick={() => onChange(item.value)} onKeyDown={event => {
    const next = event.key === 'ArrowRight' ? (index + 1) % items.length : event.key === 'ArrowLeft' ? (index + items.length - 1) % items.length : event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 : -1;
    if (next >= 0) { event.preventDefault(); onChange(items[next].value); event.currentTarget.parentElement.children[next].focus(); }
  }}>{item.label}</button>)}</div>;
}
