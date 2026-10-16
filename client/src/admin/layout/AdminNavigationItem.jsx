import { Link } from "react-router-dom";
import AppIcon from "../../components/AppIcon";
export function AdminNavigationItem({ item, active, onNavigate }) {
  return (
    <Link
      to={"/admin/" + item.path}
      className={`menu-item ${active ? "active" : ""}`}
      onClick={onNavigate}
    >
      <AppIcon name={item.icon} />
      <span>{item.title}</span>
    </Link>
  );
}
