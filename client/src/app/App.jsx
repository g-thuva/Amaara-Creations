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
  const isAdminRoute = location.pathname.startsWith("/admin");
  // Route metadata is owned by Seo so product metadata stays current.

  return (
      <div
        className={`app-container ${isAdminRoute ? "admin-app" : "storefront"}`}
      >
        <Seo />
        {isAdminRoute && (
          <a
            className="s-skip"
            href="#store-main"
            onClick={(e) => {
              e.preventDefault();
              document.getElementById("store-main")?.focus();
            }}
          >
            Skip to main content
          </a>
        )}
        {!isAdminRoute && <Navbar />}
        <main
          id="store-main"
          tabIndex="-1"
          className={isAdminRoute ? "admin-main-content" : "main-content"}
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
        {!isAdminRoute && <Footer />}
      </div>
  );
}

export default App;
