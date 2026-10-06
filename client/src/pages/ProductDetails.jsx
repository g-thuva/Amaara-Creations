import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useResource } from '../hooks/useResource';
import { productApi } from '../services/productApi';
import { cartApi } from '../services/cartApi';
import { useStore } from '../contexts/StoreContext';
import { Button, EmptyState, ErrorState, Icon, Media, Price, Quantity, Skeleton } from '../components/storefront/UI';
import Reviews from '../components/storefront/Reviews';

function Product({ product }) {
  const [selected, setSelected] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [image, setImage] = useState(0);
  const [showSticky, setShowSticky] = useState(false);
  const purchaseRef = useRef(null);
  const { cart, wishlist, pending, mutate, toggleWishlist, values } = useStore();
  const variant = product.variants?.find((item) => String(item.id) === selected);
  const stock = variant ? variant.stockQuantity : product.stock;
  const gallery = product.media?.length ? product.media : [{ url: product.imageUrl, altText: product.name }];
  const saved = wishlist.data?.items.some((item) => item.productId === product.id);
  const deliveryNote = values['delivery.note'] || values['contact.orderNote'];

  useEffect(() => {
    const element = purchaseRef.current;
    if (!element) return undefined;
    const observer = new IntersectionObserver(([entry]) => setShowSticky(!entry.isIntersecting), { threshold: 0.2 });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const addToCart = () => mutate(
    () => cartApi.addToCart(product.id, quantity),
    cart.reload,
    'Added to your cart.',
  );

  const moveImage = (direction) => {
    if (gallery.length < 2) return;
    setImage((index) => (index + direction + gallery.length) % gallery.length);
  };

  const unavailable = pending || stock <= 0 || !!variant;

  return (
    <>
      <nav className="s-breadcrumb" aria-label="Breadcrumb">
        <Link to="/">Home</Link><Icon name="chevronRight" />
        <Link to="/products">Shop</Link><Icon name="chevronRight" />
        <span aria-current="page">{product.name}</span>
      </nav>

      <div className="s-pdp">
        <section
          className="s-pdp-gallery"
          aria-label="Product gallery"
          tabIndex="0"
          onKeyDown={(event) => {
            if (event.key === 'ArrowLeft') moveImage(-1);
            if (event.key === 'ArrowRight') moveImage(1);
          }}
        >
          <Media eager src={gallery[image]?.url} alt={gallery[image]?.altText || product.name} width={1000} height={1000} />
          {gallery.length > 1 && (
            <div className="s-thumbnails" aria-label="Product images">
              {gallery.map((media, index) => (
                <button key={media.id || index} type="button" aria-label={`View image ${index + 1}`} aria-pressed={index === image} onClick={() => setImage(index)}>
                  <Media src={media.url} alt={media.altText || `${product.name}, image ${index + 1}`} width={160} height={160} />
                </button>
              ))}
            </div>
          )}
          {gallery.length > 1 && <p className="s-gallery-hint">Use the thumbnails or keyboard arrow keys to browse images.</p>}
        </section>

        <section className="s-pdp-info">
          {product.category && <p className="s-eyebrow">{product.category}</p>}
          <h1>{product.name}</h1>
          <Price value={variant?.priceOverride ?? product.price} />
          <p className={`s-availability ${stock > 0 ? 'available' : 'unavailable'}`}>
            <Icon name={stock > 0 ? 'check' : 'x'} />{stock > 0 ? 'In stock' : 'Out of stock'}
          </p>
          {product.shortDescription && <p className="s-lead">{product.shortDescription}</p>}

          {product.variants?.length > 0 && (
            <label className="s-field">
              Product option
              <select value={selected} onChange={(event) => { setSelected(event.target.value); setQuantity(1); }}>
                <option value="">Standard product</option>
                {product.variants.map((item) => (
                  <option key={item.id} value={item.id} disabled={item.stockQuantity <= 0}>
                    {item.name}{item.stockQuantity <= 0 ? ' — out of stock' : ''}
                  </option>
                ))}
              </select>
            </label>
          )}

          {variant && (
            <p className="s-alert">
              This option is available to view. The current cart cannot store selected variants. <Link to="/contact">Contact us to order this option.</Link>
            </p>
          )}

          <div ref={purchaseRef} className="s-purchase">
            <Quantity value={quantity} onChange={setQuantity} max={Math.max(stock, 1)} disabled={pending || stock <= 0} />
            <Button disabled={unavailable} onClick={addToCart}>
              <Icon name="bag" />{pending ? 'Please wait…' : stock <= 0 ? 'Out of stock' : variant ? 'Contact us to order' : 'Add to cart'}
            </Button>
          </div>
          <Button variant="secondary" aria-pressed={!!saved} disabled={pending || wishlist.loading} onClick={() => toggleWishlist(product.id)}>
            <Icon name="heart" fill={saved ? 'currentColor' : 'none'} />{saved ? 'Saved to wishlist' : 'Save to wishlist'}
          </Button>

          {deliveryNote && <p className="s-delivery-note"><Icon name="truck" />{deliveryNote}</p>}

          <div className="s-product-accordions">
            <details open>
              <summary>About this product</summary>
              <p>{product.description || 'No additional description is available.'}</p>
            </details>
            {product.variants?.length > 0 && (
              <details>
                <summary>Available options</summary>
                <ul>{product.variants.map((item) => <li key={item.id}>{item.name} — {item.stockQuantity > 0 ? 'available' : 'out of stock'}</li>)}</ul>
              </details>
            )}
          </div>
        </section>
      </div>

      <Reviews productId={product.id} />

      {showSticky && (
        <div className="s-mobile-purchase" aria-label="Product purchase actions">
          <div><strong>{product.name}</strong><Price value={variant?.priceOverride ?? product.price} /></div>
          <Button disabled={unavailable} onClick={addToCart}>{variant ? 'Contact to order' : stock <= 0 ? 'Out of stock' : 'Add to cart'}</Button>
        </div>
      )}
    </>
  );
}

export default function ProductDetails() {
  const { id } = useParams();
  const resource = useResource((signal) => productApi.getProductById(id, signal), id);

  return (
    <div className="s-container s-page">
      {resource.loading ? <Skeleton count={2} /> : resource.error?.response?.status === 404 ? (
        <section className="s-not-found">
          <h1>Product not found</h1>
          <EmptyState title="This product is not available" label="Return to the shop">
            <p>The link may be old, or the product may no longer be published.</p>
            <Link className="s-text-link" to="/">Go to the homepage</Link>
          </EmptyState>
        </section>
      ) : resource.error ? (
        <ErrorState error={resource.error} retry={resource.reload} />
      ) : (
        <Product key={id} product={resource.data} />
      )}
    </div>
  );
}
