import React from 'react';
import ReactDOM from 'react-dom/client';
import App from '@/app/App.jsx';
import { AppProviders } from '@/app/providers/AppProviders.jsx';
import '@/shared/styles/fonts.css';
import '@/shared/styles/tokens.css';
import '@/shared/styles/globals.css';
import '@/app/layout/storefrontLayout.css';
import '@/shared/styles/studio.css';

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode><AppProviders><App /></AppProviders></React.StrictMode>
);
