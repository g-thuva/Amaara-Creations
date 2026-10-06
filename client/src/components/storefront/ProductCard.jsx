import { Link } from 'react-router-dom';
import { useStore } from '../../contexts/StoreContext';
import { Icon, Media, Price } from './UI';

export default function ProductCard({ product, eager = false }) {
  const { wishlist, toggleWishlist, pending } = useStore();
  const saved = !!wishlist.data?.items.some((item) => item.productId === product.id);
  const outOfStock = product.isOutOfStock || product.stock <= 0;
  const lowStock = !outOfStock && product.stock <= 5;
  const media = product.media?.find((item) => item.isPrimary) || product.media?.[0];

  return (
    <article className="s-product-card">
      <Link className="s-product-card-link" to={`/products/${product.id}`} aria-label={`View ${product.name}`} />
      <div className="s-product-visual">
        <Media
          src={media?.url || product.imageUrl}
          alt={media?.altText || product.name}
          eager={eager}
          width={800}
          height={600}
        />
        <div className="s-product-badges">
          {outOfStock && <span className="s-stock-label is-out">Out of stock</span>}
          {lowStock && <span className="s-stock-label is-low">Low stock</span>}
        </div>
        <button
          type="button"
          className="s-save"
          aria-label={`${saved ? 'Remove' : 'Save'} ${product.name} ${saved ? 'from' : 'to'} wishlist`}
          aria-pressed={saved}
          disabled={pending || wishlist.loading}
          onClick={() => toggleWishlist(product.id)}
        >
          <Icon name="heart" fill={saved ? 'currentColor' : 'none'} />
        </button>
      </div>
      <div className="s-product-info">
        {product.category && <p className="s-meta">{product.category}</p>}
        <h3 title={product.name}>{product.name}</h3>
        <Price value={product.price} />
        {product.reviewCount > 0 && (
          <span className="s-card-rating" aria-label={`${product.rating} out of 5 stars from ${product.reviewCount} reviews`}>
            <Icon name="star" fill="currentColor" /> {product.rating} ({product.reviewCount})
          </span>
        )}
      </div>
    </article>
  );
}
