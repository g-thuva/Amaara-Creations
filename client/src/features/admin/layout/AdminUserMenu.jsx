import { useContext, useEffect, useRef, useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { AdminSessionActions } from './AdminSessionActions.js';
export function AdminUserMenu({ user, logout }) {
  const [open, setOpen] = useState(false); const root = useRef(null); const trigger = useRef(null);
  const session = useContext(AdminSessionActions);
  const roles = user?.roles?.join(', ') || user?.role || 'Staff';
  useEffect(() => {
    if (!open) return undefined;
    root.current.querySelector('[role=menuitem]')?.focus();
    const outside = event => { if (!root.current?.contains(event.target)) setOpen(false); };
    document.addEventListener('pointerdown', outside);
    return () => document.removeEventListener('pointerdown', outside);
  }, [open]);
  const keyboard = event => {
    if (event.key === 'Escape') { event.preventDefault(); setOpen(false); trigger.current?.focus(); }
    if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
      event.preventDefault(); const items = [...root.current.querySelectorAll('[role=menuitem]')]; const index = items.indexOf(document.activeElement);
      const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 : (index + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length;
      items[next]?.focus();
    }
  };
  return <div className="user-menu" ref={root} onKeyDown={keyboard} onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false); }}><button ref={trigger} type="button" className="user-menu-trigger" aria-haspopup="menu" aria-expanded={open} onClick={() => setOpen(!open)} onKeyDown={event => { if (event.key === 'ArrowDown' && !open) { event.preventDefault(); setOpen(true); } }}><span className="admin-sr-only">Open user menu: </span><span className="user-avatar" aria-hidden="true">{user?.name?.charAt(0).toUpperCase() || 'A'}</span><span className="user-info"><span className="user-name">{user?.name || 'Staff member'}</span>{' '}<span className="user-role">{roles}</span></span><AppIcon name="chevronDown" /></button>{open && <div className="user-dropdown" role="menu" aria-label="Your account"><button role="menuitem" onClick={session?.reauthenticate || logout}><AppIcon name="lock" />Sign out / reauthenticate</button><button role="menuitem" onClick={logout}><AppIcon name="logout" />Logout</button></div>}</div>;
}
