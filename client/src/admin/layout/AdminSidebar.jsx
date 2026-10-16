import AppIcon from "../../components/AppIcon";
import { AdminNavigationItem } from "./AdminNavigationItem.jsx";
export function AdminSidebar({ navigation }) {
  const { sidebarOpen, isMobile, items, location, close, logout, escape } =
    navigation;
  return (
    <aside
      id="admin-navigation"
      inert={isMobile && !sidebarOpen ? true : undefined}
      aria-hidden={isMobile && !sidebarOpen ? true : undefined}
      className={`admin-sidebar ${sidebarOpen ? "open" : ""}`}
      onKeyDown={escape}
    >
      <div className="sidebar-header">
        <h2>
          <span style={{ color: "#d4a76a" }}>Amaara</span> Admin
        </h2>
      </div>
      <nav className="sidebar-menu" aria-label="Administration">
        {items.map((item) => (
          <AdminNavigationItem
            key={item.path}
            item={item}
            active={location.pathname === "/admin/" + item.path}
            onNavigate={() => {
              if (isMobile) close();
            }}
          />
        ))}
        <button type="button" className="menu-item" onClick={logout}>
          <AppIcon name="logout" />
          <span>Logout</span>
        </button>
      </nav>
    </aside>
  );
}
