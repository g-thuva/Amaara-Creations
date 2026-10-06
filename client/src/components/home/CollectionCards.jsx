import { Link } from 'react-router-dom';
import { Icon, Media } from '../storefront/UI';

export default function CollectionCards({ collections = [] }) {
  if (!collections.length) return null;
  return (
    <div className="s-collection-grid">
      {collections.map((collection) => (
        <Link className="s-collection-card" key={collection.id} to={`/products?collectionId=${collection.id}`}>
          {collection.imageUrl ? (
            <Media src={collection.imageUrl} alt={collection.name} width={900} height={600} />
          ) : (
            <div className="s-collection-fallback" aria-hidden="true"><span /><span /><span /></div>
          )}
          <div>
            <p>{collection.productCount} {collection.productCount === 1 ? 'product' : 'products'}</p>
            <h3>{collection.name}</h3>
            {collection.description && <span>{collection.description}</span>}
            <strong>View collection <Icon name="arrow" /></strong>
          </div>
        </Link>
      ))}
    </div>
  );
}
