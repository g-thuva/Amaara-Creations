import { useState } from 'react';
import { useWishlist } from '@/features/wishlist/WishlistProvider.jsx';
import { useCart } from '@/features/cart/CartProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { cartApi } from '@/features/cart/cartApi.js';
import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
import { Media } from '@/shared/components/ui/Media.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';

const defaultSwatches = [
  { name: 'Frosted Clear', color: '#e8f0f8' },
  { name: 'Mirror Gold', color: 'linear-gradient(135deg, #e5c57e, #b88e44)' },
  { name: 'Classic White', color: '#ffffff' },
  { name: 'Natural Wood', color: '#c8a882' },
];

export default function ProductCard({ product, eager = false }) {
  const { wishlist, toggleWishlist, pending } = useWishlist?.() || { wishlist: { data: { items: [] } } };
  const cartContext = useCart?.();
  const cart = cartContext?.cart;
  const { mutate, pending: cartPending } = useAuthenticatedMutation?.() || { mutate: () => Promise.resolve(false), pending: false };
  const [added, setAdded] = useState(false);

  const saved = !!wishlist?.data?.items?.some((item) => item.productId === product.id);
  const outOfStock = product.isOutOfStock || product.stock <= 0;
  const lowStock = !outOfStock && product.stock <= 5;
  const media = product.media?.find((item) => item.isPrimary) || product.media?.[0];
  const originalPrice = product.originalPrice || (product.price ? Math.round(product.price * 1.18) : null);
  const swatches = product.swatches || defaultSwatches;
  const categoryLabel = product.category || 'Wedding Collection';

  const handleAddToCart = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (outOfStock || cartPending) return;
    mutate(
      () => cartApi.addToCart(product.id, 1, product.variants?.[0]?.id ?? null),
      () => {
        cart?.reload?.();
        setAdded(true);
        setTimeout(() => setAdded(false), 2200);
      },
      <>
        Added to your cart.{' '}
        <Link to="/cart" style={{ textDecoration: 'underline', fontWeight: 600 }}>
          View cart →
        </Link>
      </>,
    );
  };

  return (
    <article className="s-product-card">
      <div className="s-product-visual">
        <Link className="s-product-card-link" to={`/products/${product.id}`} aria-label={`View ${product.name}`}>
          <Media
            src={media?.url || product.imageUrl}
            alt={media?.altText || product.name}
            eager={eager}
            width={800}
            height={800}
          />
        </Link>
        <div className="s-product-badges">
          {outOfStock ? (
            <span className="s-stock-label is-out">Out of stock</span>
          ) : lowStock ? (
            <span className="s-stock-label is-low">Low stock</span>
          ) : (
            <span className="s-discount-badge">Save 15%</span>
          )}
        </div>

        <div className="s-product-actions">
          <button
            type="button"
            className="s-save"
            aria-label={`${saved ? 'Remove' : 'Save'} ${product.name} ${saved ? 'from' : 'to'} wishlist`}
            aria-pressed={saved}
            disabled={pending || wishlist?.loading}
            onClick={() => toggleWishlist?.(product.id)}
            title={saved ? 'In wishlist' : 'Save to wishlist'}
          >
            <Icon name="heart" fill={saved ? '#2d559a' : 'none'} color={saved ? '#2d559a' : 'currentColor'} />
          </button>

          <button
            type="button"
            className={`s-action-cart ${added ? 'is-added' : ''}`}
            aria-label={`Add ${product.name} to cart`}
            disabled={outOfStock || cartPending}
            onClick={handleAddToCart}
            title={outOfStock ? 'Out of stock' : added ? 'Added to cart' : 'Add to cart'}
          >
            <Icon name={added ? 'check' : 'cart'} />
          </button>
        </div>
      </div>

      <div className="s-product-info">
        <div className="s-swatch-row" aria-label="Available finishes">
          {swatches.map((swatch, i) => (
            <span
              key={i}
              className="s-swatch-dot"
              title={swatch.name}
              style={{ background: swatch.color }}
            />
          ))}
        </div>

        <h3 title={product.name}>
          <Link to={`/products/${product.id}`}>{product.name}</Link>
        </h3>

        <div className="s-price-row">
          <Price value={product.price} currency={product.currency} />
          {originalPrice && !outOfStock && (
            <span className="s-original-price">
              <Price value={originalPrice} currency={product.currency} />
            </span>
          )}
          <span className="s-comp-value">Comp. value <span aria-hidden="true">ⓘ</span></span>
        </div>

        <div className="s-product-footer">
          <span className="s-collection-tag">
            <Icon name="layers" /> {categoryLabel}
          </span>
          <Link className="s-more-link" to={`/products/${product.id}`}>
            Customise <span>›</span>
          </Link>
        </div>
      </div>
    </article>
  );
}
