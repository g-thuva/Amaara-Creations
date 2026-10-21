import { useRoutes } from 'react-router-dom';
import { publicRoutes } from './PublicRoutes.jsx';
import { accountRoutes } from './AccountRoutes.jsx';
import NotFoundPage from '@/app/pages/NotFoundPage.jsx';

export default function AppRoutes() {
  return useRoutes([
    ...publicRoutes,
    ...accountRoutes,
    { path: '*', element: <NotFoundPage /> },
  ]);
}
