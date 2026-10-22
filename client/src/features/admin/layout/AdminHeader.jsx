import { Link } from 'react-router-dom';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { AdminUserMenu } from './AdminUserMenu.jsx';
export function AdminHeader({ navigation }) {
  const { navigationToggle, sidebarOpen, toggle, title, user, group, current, location } = navigation;
  const detail = current && location.pathname !== '/admin/' + current.path;
  return <header className="admin-header"><div className="header-left">
    <button ref={navigationToggle} className="toggle-sidebar" aria-label="Toggle admin navigation" aria-controls="admin-navigation" aria-expanded={sidebarOpen} onClick={toggle}><AppIcon name="menu" /></button>
    {detail && <Link className="back-button" to={'/admin/' + current.path} aria-label={'Back to ' + title}><AppIcon name="arrowLeft" /></Link>}
    <nav className="admin-breadcrumbs" aria-label="Breadcrumb"><ol><li>{group || 'Admin'}<AppIcon name="chevronRight" /></li><li>{detail ? <Link to={'/admin/' + current.path}>{title}</Link> : <span aria-current="page">{title}</span>}</li>{detail && <li aria-current="page">Details</li>}</ol></nav>
  </div><AdminUserMenu user={user} logout={navigation.logout} /></header>;
}
