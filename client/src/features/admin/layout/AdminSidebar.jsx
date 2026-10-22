import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { AdminNavigationItem } from './AdminNavigationItem.jsx';
export function AdminSidebar({ navigation }) {
  const { sidebarOpen, isMobile, groups, location, close, logout, escape, user } = navigation;
  const sidebar = useRef(null);
  useEffect(() => { if (isMobile && sidebarOpen) sidebar.current?.querySelector('button,a[href]')?.focus(); }, [isMobile, sidebarOpen]);
  const roles = user?.roles?.join(', ') || user?.role || 'Staff';
  return <aside ref={sidebar} id="admin-navigation" role={isMobile && sidebarOpen ? 'dialog' : undefined} aria-modal={isMobile && sidebarOpen ? true : undefined} aria-label="Admin navigation" inert={isMobile && !sidebarOpen ? true : undefined} aria-hidden={isMobile && !sidebarOpen ? true : undefined} className={`admin-sidebar ${sidebarOpen ? 'open' : ''}`} onKeyDown={escape}>
    <div className="sidebar-header"><Link className="sidebar-brand" to="/admin" aria-label="Amaara Admin"><strong>Amaara</strong> Admin</Link><span className="brand-mark" aria-hidden="true">A</span>{isMobile && <button type="button" className="menu-item" aria-label="Close admin navigation" onClick={() => { close(); navigation.navigationToggle.current?.focus(); }}><AppIcon name="x" /></button>}</div>
    <nav className="sidebar-menu" aria-label="Administration">{groups.map(group => <div className="nav-group" key={group.title}><h2>{group.title}</h2>{group.items.map(item => <AdminNavigationItem key={item.path} item={item} active={location.pathname === '/admin/' + item.path || location.pathname.startsWith('/admin/' + item.path + '/')} onNavigate={() => { if (isMobile) close(); }} />)}</div>)}</nav>
    <div className="sidebar-footer"><div className="sidebar-user"><span className="user-avatar" aria-hidden="true">{user?.name?.charAt(0).toUpperCase() || 'A'}</span><div className="sidebar-user-info"><strong>{user?.name || 'Staff member'}</strong><small title={roles}>{roles}</small></div></div><button type="button" className="menu-item" onClick={logout} aria-label="Logout" title="Logout"><AppIcon name="logout" /><span className="nav-label">Logout</span></button></div>
  </aside>;
}
