import { Link } from 'react-router-dom';
import { EmptyState } from '@/features/admin/shared/components/EmptyState.jsx';

export function TopProductsList({ topProducts, canManageCatalog = true }) {
  return (
    <section className="dashboard-card">
      <div className="card-header">
        <h2>Top Selling Products</h2>
        {canManageCatalog && <Link to="/admin/products" className="view-all">View All</Link>}
      </div>
      {!topProducts.length ? (
        <EmptyState title="No sales to show yet" description="Your bestselling products will appear after orders are confirmed." icon="package" />
      ) : (
        <div className="product-list">
          {topProducts.slice(0, 5).map((product, index) => (
            <div className="product-item" key={product.productId || index}>
              <span className="product-rank">{String(index + 1).padStart(2, '0')}</span>
              <div className="product-info">
                <h3>{product.productName}</h3>
                <span className="product-meta">{product.totalQuantitySold} sales</span>
              </div>
              {Number.isFinite(product.totalQuantitySold) && topProducts[0]?.totalQuantitySold > 0 && (
                <progress value={product.totalQuantitySold} max={topProducts[0].totalQuantitySold} aria-label={product.productName + ' sales compared with top product'} />
              )}
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
