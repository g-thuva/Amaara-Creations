import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.jsx';
import { BrowserRouter, HashRouter } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import './index.css';
import './styles/storefront.css';

const Router = import.meta.env.VITE_ROUTER_MODE === 'hash' ? HashRouter : BrowserRouter;
if (import.meta.env.VITE_ROUTER_MODE !== 'hash' && window.location.hash.startsWith('#/')) {
  window.history.replaceState(null, '', import.meta.env.BASE_URL + window.location.hash.slice(2));
}
const routerProps = Router === BrowserRouter ? { basename: import.meta.env.BASE_URL.replace(/\/$/, '') || '/' } : {};

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <Router {...routerProps}>
      <AuthProvider>
        <App />
      </AuthProvider>
    </Router>
  </React.StrictMode>
);
