import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '@/features/auth/AuthProvider.jsx';
import { ToastProvider } from '@/shared/components/feedback/ToastProvider.jsx';
import { StaffApp } from '@/features/admin/app/StaffApp.jsx';
import '@/shared/styles/fonts.css';
import '@/shared/styles/tokens.css';
import '@/shared/styles/globals.css';
import '@/features/auth/auth.css';
import '@/shared/styles/studio.css';

ReactDOM.createRoot(document.getElementById('root')).render(<React.StrictMode><BrowserRouter basename={import.meta.env.BASE_URL.replace(/\/$/, '') || '/'}><AuthProvider><ToastProvider><StaffApp /></ToastProvider></AuthProvider></BrowserRouter></React.StrictMode>);