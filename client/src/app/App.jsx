import { Suspense } from 'react';
import { useLocation } from 'react-router-dom';
import AppRoutes from '@/app/routes/AppRoutes.jsx';
import Navbar from '@/app/layout/Navbar.jsx';
import Footer from '@/app/layout/Footer.jsx';
import Seo from '@/app/seo/Seo.jsx';
import ErrorBoundary from '@/shared/components/feedback/ErrorBoundary.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import { reportClientError } from '@/shared/api/clientTelemetry.js';
function App() {
  const location = useLocation();
  return (
      <div className="app-container storefront">
        <Seo />
        <Navbar />
        <main
          id="store-main"
          tabIndex="-1"
          className="main-content"
        >
          <ErrorBoundary
            onError={error => reportClientError('storefront.render', error)}
            key={
              /^\/custom(?:\/\d+)?$/.test(location.pathname)
                ? "/custom"
                : location.pathname
            }
          >
            <Suspense fallback={<Skeleton />}>
              <AppRoutes />
            </Suspense>
          </ErrorBoundary>
        </main>
        <Footer />
      </div>
  );
}

export default App;
