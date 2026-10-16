import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { adminNavigation } from "../app/adminNavigation.js";
import { can } from "../app/adminPermissions.js";
export function useAdminNavigation() {
  const navigationToggle = useRef(null);
  const [sidebarOpen, setSidebarOpen] = useState(() => window.innerWidth > 992);
  const [isMobile, setIsMobile] = useState(() => window.innerWidth <= 992);
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout: authLogout } = useAuth();
  useEffect(() => {
    const media = window.matchMedia("(max-width: 992px)");
    const changed = () => setIsMobile(media.matches);
    media.addEventListener("change", changed);
    return () => media.removeEventListener("change", changed);
  }, []);
  useEffect(() => {
    if (isMobile) setSidebarOpen(false);
  }, [isMobile]);
  useEffect(() => {
    const outside = (event) => {
      if (
        isMobile &&
        sidebarOpen &&
        !event.target.closest(".admin-sidebar") &&
        !navigationToggle.current?.contains(event.target)
      )
        setSidebarOpen(false);
    };
    document.addEventListener("mousedown", outside);
    return () => document.removeEventListener("mousedown", outside);
  }, [isMobile, sidebarOpen]);
  const logout = async () => {
    await authLogout();
    navigate("/login");
  };
  const close = () => setSidebarOpen(false);
  const escape = (event) => {
    if (isMobile && event.key === "Escape") {
      close();
      navigationToggle.current?.focus();
    }
  };
  const items = adminNavigation.filter(
    (item) => item.navigation !== false && can(user, item.permission),
  );
  const title =
    items.find(
      (item) =>
        location.pathname === "/admin/" + item.path ||
        location.pathname.startsWith("/admin/" + item.path + "/"),
    )?.title || "Dashboard";
  return {
    navigationToggle,
    sidebarOpen,
    isMobile,
    user,
    items,
    title,
    location,
    navigate,
    logout,
    close,
    escape,
    toggle: () => setSidebarOpen((open) => !open),
  };
}
