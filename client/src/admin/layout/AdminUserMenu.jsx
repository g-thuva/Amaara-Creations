import styles from "./AdminLayout.module.css";
export function AdminUserMenu({ user }) {
  return (
    <div className={styles.userMenu}>
      <div className="user-info">
        <div className="user-name">{user?.name || "Admin"}</div>
        <div className="user-role">{user?.role || "Administrator"}</div>
      </div>
      <div className="user-avatar">
        {user?.name?.charAt(0).toUpperCase() || "A"}
      </div>
    </div>
  );
}
