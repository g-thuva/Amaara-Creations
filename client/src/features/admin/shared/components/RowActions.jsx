import AppIcon from '@/shared/components/ui/Icon.jsx';
export function RowActions({ label = 'Row actions', children }) {
  return <details className="admin-row-actions" onKeyDown={event => { if (event.key === 'Escape') { event.preventDefault(); event.currentTarget.open = false; event.currentTarget.querySelector('summary')?.focus(); } }}><summary role="button" aria-label={label}>Actions <AppIcon name="chevronDown" /></summary><div role="group" aria-label={label} onClick={event => { if (event.target.closest('button')) event.currentTarget.parentElement.open = false; }}>{children}</div></details>;
}
