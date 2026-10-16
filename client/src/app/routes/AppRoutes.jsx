import { lazy } from 'react';
import { useRoutes } from 'react-router-dom';
import { publicRoutes } from './PublicRoutes.jsx';
import { accountRoutes } from './AccountRoutes.jsx';
import NotFoundPage from '@/app/pages/NotFoundPage.jsx';

const AdminRoutes = lazy(() => import('./AdminRoutes.jsx'));
export default function AppRoutes() {
  return useRoutes([
    ...publicRoutes,
    ...accountRoutes,
    { path: '/admin/*', element: <AdminRoutes /> },
    { path: '*', element: <NotFoundPage /> },
  ]);
}
