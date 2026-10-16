import AppIcon from "../../components/AppIcon";
import { AdminUserMenu } from "./AdminUserMenu.jsx";
import styles from "./AdminLayout.module.css";
export function AdminHeader({ navigation }) {
  const { navigationToggle, sidebarOpen, toggle, navigate, title, user } =
    navigation;
  return (
    <header className="admin-header">
      <div className={styles.headerLeft}>
        <button
          ref={navigationToggle}
          className="toggle-sidebar"
          aria-label="Toggle admin navigation"
          aria-controls="admin-navigation"
          aria-expanded={sidebarOpen}
          onClick={toggle}
        >
          <AppIcon name={sidebarOpen ? "x" : "menu"} />
        </button>
        <button
          className="back-button"
          onClick={() => navigate("/")}
          title="Back to Home"
        >
          <AppIcon name="arrowLeft" />
        </button>
        <h3 style={{ margin: 0, color: "#2d3748" }}>{title}</h3>
      </div>
      <AdminUserMenu user={user} />
    </header>
  );
}
