import { useState } from 'react';
import { Link } from 'react-router-dom';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { resolveMediaUrl } from '@/shared/api/requestConfig.js';
import { EmptyState } from '@/features/admin/shared/components/EmptyState.jsx';
import { ErrorState } from '@/features/admin/shared/components/ErrorState.jsx';
import { shareOfTotal } from '@/utils/percent.js';
import { formatMoney } from '@/utils/formatMoney.js';
import { productSnapshot } from './dashboardAdapter.js';

function ProductThumbnail({ product }) {
  const [failed, setFailed] = useState(false);
  const source = resolveMediaUrl(product.productImageUrl);
  return <span className="sales-product-image">{source && !failed ? <img src={source} alt="" loading="lazy" onError={() => setFailed(true)} /> : <AppIcon name="package" />}</span>;
}
export function TopProductsList({ resource, canManageCatalog = true }) {
  const products = productSnapshot(resource.data);
  const shares = shareOfTotal(products.map(product => product.totalRevenue));
  return <section className="sales-panel sales-products"><div className="sales-panel-header"><div><h2>Top selling products</h2><p>All time · share of returned top 10 revenue</p></div>{canManageCatalog && <Link to="/admin/products">View all <span aria-hidden="true">↗</span></Link>}</div>
    <div className="sales-scroll" tabIndex={0} role="region" aria-label="Top selling products list">{resource.loading ? <div className="sales-products-skeleton" role="status" aria-label="Loading top products">{Array.from({ length: 5 }, (_, index) => <span className="admin-skeleton" key={index} />)}</div> : resource.error ? <ErrorState title="Top products could not be loaded" retry={resource.reload} /> : !products.length ? <EmptyState title="No products sold yet" description="Your bestselling products will appear here." icon="package" /> : <ol className="sales-product-list">{products.slice(0, 5).map((product, index) => <li className="sales-product-row" key={`${product.productId}-${product.productName}-${index}`}>
      <span className="sales-product-rank">{index + 1}</span><ProductThumbnail product={product} /><div className="sales-product-info"><h3 title={product.productName}>{product.productName}</h3><div className="sales-product-meta"><span>{product.totalQuantitySold.toLocaleString('en-GB')} units sold</span><span>{formatMoney(product.totalRevenue)}</span></div><div className="sales-product-share"><svg className="sales-share-bar" viewBox="0 0 100 4" preserveAspectRatio="none" aria-hidden="true"><rect width="100" height="4" rx="2" /><rect width={shares[index]} height="4" rx="2" /></svg><span>{shares[index].toFixed(1)}%</span></div></div>
    </li>)}</ol>}</div>
  </section>;
}
