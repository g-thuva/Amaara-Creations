import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import { catalogParams } from '@/features/catalog/catalogQuery.ts';
import { Pagination } from './ProductPagination.jsx';
import ProductCard from './ProductCard.jsx';

export default function CatalogResults({ catalog }) {
  const { data, params, clear, update } = catalog;
  return (
          <section aria-label="Products" aria-busy={data.loading}>
            {data.loading ? <Skeleton count={12} /> : data.error ? <ErrorState error={data.error} retry={data.reload} /> : (
              <>
                <p className="s-result-count" role="status">{data.data?.totalCount || 0} products</p>
                {data.data?.products.length ? (
                  <div className="s-product-grid s-catalog-grid">
                    {data.data.products.map((product, index) => <ProductCard key={product.id} product={product} eager={index < 3} />)}
                  </div>
                ) : (
                  <EmptyState title={data.data?.publishedCount === 0 ? 'Begin with your own design' : 'No products match those filters'} to={null}>
                    <p>{data.data?.publishedCount === 0 ? 'Our ready-made collection is being prepared. In the meantime, you can design your own piece in the studio.' : 'Try a broader search, clear the active filters, or design your own piece.'}</p>
                    <div className="s-actions">
                      {data.data?.publishedCount !== 0 && <Button onClick={clear}>Clear filters</Button>}
                      <Link className="s-button s-button-secondary" to="/custom">Design your own</Link>
                    </div>
                  </EmptyState>
                )}
                <Pagination page={Number(catalogParams(params).pageNumber)} totalPages={data.data?.totalPages || 0} onChange={(page) => update({ page: String(page) }, false)} />
              </>
            )}
          </section>
  );
}
