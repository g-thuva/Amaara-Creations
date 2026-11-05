import { Link, useNavigate, useParams } from 'react-router-dom';
import { useResource } from '@/shared/hooks/useResource.js';
import { catalogApi } from '@/features/catalog/catalogApi.js';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import { ProductSeo } from '@/features/catalog/components/ProductSeo.jsx';
import ProductDetail from '@/features/catalog/components/ProductDetail.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
export default function ProductDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const resource = useResource((signal) => catalogApi.getProductById(id, signal), id);

  return (
    <div className="s-container s-page">
      {resource.loading ? <Skeleton count={2} /> : resource.error?.response?.status === 404 ? (
        <section className="s-not-found">
          <h1>Product not found</h1>
          <EmptyState to="/products" title="This product is not available" label="Return to the shop">
            <p>The link may be old, or the product may no longer be published.</p>
            <Link className="s-text-link" to="/">Go to the homepage</Link>
          </EmptyState>
        </section>
      ) : resource.error ? (
        <ErrorState error={resource.error} retry={resource.reload} />
      ) : (
        <>
          <ProductSeo product={resource.data} />
          <Button type="button" variant="secondary" className="s-product-back" onClick={() => navigate(-1)}>
            <Icon name="arrowLeft" /> Previous
          </Button>
          <ProductDetail key={id} product={resource.data} />
        </>
      )}
    </div>
  );
}
