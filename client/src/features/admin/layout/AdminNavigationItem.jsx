import { Link } from "react-router-dom";
import AppIcon from "@/shared/components/ui/Icon.jsx";
export function AdminNavigationItem({ item, active, onNavigate }) {
  return (
    <Link
      to={"/admin/" + item.path}
      className={`menu-item ${active ? "active" : ""}`}
      aria-current={active ? 'page' : undefined}
      aria-label={item.title}
      title={item.title}
      onClick={onNavigate}
    >
      <AppIcon name={item.icon} />
      <span className="nav-label">{item.title}</span>
    </Link>
  );
}
