import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import { adminNavigation } from "@/features/admin/app/adminNavigation.js";
import { can } from "@/app/config/permissions.js";
import { navigationGroups } from './navigationGroups.js';
export function useAdminNavigation() {
  const navigationToggle = useRef(null);
  const [sidebarOpen, setSidebarOpen] = useState(() => window.innerWidth >= 1280);
  const [isMobile, setIsMobile] = useState(() => window.innerWidth < 768);
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout: authLogout } = useAuth();
  useEffect(() => {
    const media = window.matchMedia("(max-width: 767px)");
    const wide = window.matchMedia('(min-width: 1280px)');
    const changed = () => { setIsMobile(media.matches); setSidebarOpen(wide.matches); };
    media.addEventListener("change", changed);
    wide.addEventListener('change', changed);
    return () => { media.removeEventListener("change", changed); wide.removeEventListener('change', changed); };
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
    await authLogout(() => navigate("/login", { replace: true }));
  };
  const close = () => { setSidebarOpen(false); if (isMobile) requestAnimationFrame(() => navigationToggle.current?.focus()); };
  const escape = (event) => {
    if (isMobile && event.key === "Escape") {
      close();
      navigationToggle.current?.focus();
    }
    if (isMobile && sidebarOpen && event.key === 'Tab') {
      const controls = [...event.currentTarget.querySelectorAll('a[href],button:not(:disabled)')].filter(node => node.getClientRects().length);
      if (event.shiftKey && document.activeElement === controls[0]) { event.preventDefault(); controls.at(-1)?.focus(); }
      if (!event.shiftKey && document.activeElement === controls.at(-1)) { event.preventDefault(); controls[0]?.focus(); }
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
  const groups = navigationGroups.map(group => ({ ...group, items: group.paths.map(path => items.find(item => item.path === path)).filter(Boolean) })).filter(group => group.items.length);
  const current = items.find(item => location.pathname === '/admin/' + item.path || location.pathname.startsWith('/admin/' + item.path + '/'));
  const group = navigationGroups.find(group => group.paths.includes(current?.path));
  return {
    navigationToggle,
    sidebarOpen,
    isMobile,
    user,
    items,
    title,
    group: group?.title,
    current,
    groups,
    location,
    navigate,
    logout,
    close,
    escape,
    toggle: () => setSidebarOpen((open) => !open),
  };
}
