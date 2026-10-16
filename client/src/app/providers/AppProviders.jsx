import { BrowserRouter, HashRouter } from 'react-router-dom';
import { AuthProvider } from '@/features/auth/AuthProvider.jsx';
import { StorefrontProviders } from './StorefrontProviders.jsx';

const Router = import.meta.env.VITE_ROUTER_MODE === 'hash' ? HashRouter : BrowserRouter;
// Preserve existing emailed hash links when deploying with browser history routing.
if (import.meta.env.VITE_ROUTER_MODE !== 'hash' && window.location.hash.startsWith('#/')) {
  window.history.replaceState(null, '', import.meta.env.BASE_URL + window.location.hash.slice(2));
}
const routerProps = Router === BrowserRouter ? { basename: import.meta.env.BASE_URL.replace(/\/$/, '') || '/' } : {};

export function AppProviders({ children }) {
  return <Router {...routerProps}>
    <AuthProvider><StorefrontProviders>{children}</StorefrontProviders></AuthProvider>
  </Router>;
}
