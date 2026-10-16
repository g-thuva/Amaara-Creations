import React from 'react';
import ReactDOM from 'react-dom/client';
import App from '@/app/App.jsx';
import { AppProviders } from '@/app/providers/AppProviders.jsx';
import '@/shared/styles/tokens.css';
import '@/shared/styles/globals.css';
import '@/app/layout/storefrontLayout.css';
import '@/features/home/home.css';
import '@/features/catalog/catalog.css';
import '@/features/reviews/reviews.css';
import '@/features/cart/cart.css';
import '@/features/wishlist/wishlist.css';
import '@/features/checkout/checkout.css';
import '@/features/account/account.css';
import '@/features/orders/orders.css';
import '@/features/content/content.css';
import '@/features/custom-design/customDesign.css';

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode><AppProviders><App /></AppProviders></React.StrictMode>
);
