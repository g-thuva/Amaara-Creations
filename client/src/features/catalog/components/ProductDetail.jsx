import { useCart } from '@/features/cart/CartProvider.jsx';
import { useWishlist } from '@/features/wishlist/WishlistProvider.jsx';
import { useSiteContent } from '@/features/content/SiteContentProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import ProductGallery from '@/features/catalog/components/ProductGallery.jsx';
import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { cartApi } from '@/features/cart/cartApi.js';
import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
import { Quantity } from '@/shared/components/ui/Quantity.jsx';
import Reviews from '@/features/reviews/components/ProductReviews.jsx';

export default function ProductDetail({ product }) {
  const [selected, setSelected] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [image, setImage] = useState(0);
  const [showSticky, setShowSticky] = useState(false);
  const purchaseRef = useRef(null);
  const { cart } = useCart();
  const { wishlist, toggleWishlist } = useWishlist();
  const { values } = useSiteContent();
  const { mutate, pending } = useAuthenticatedMutation();
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
        <ProductGallery product={product} gallery={gallery} image={image} setImage={setImage} moveImage={moveImage} />

        <section className="s-pdp-info">
          {product.category && <p className="s-eyebrow">{product.category}</p>}
          <h1>{product.name}</h1>
          <Price value={variant?.priceOverride ?? product.price} currency={product.currency} />
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
          <div><strong>{product.name}</strong><Price value={variant?.priceOverride ?? product.price} currency={product.currency} /></div>
          <Button disabled={unavailable} onClick={addToCart}>{variant ? 'Contact to order' : stock <= 0 ? 'Out of stock' : 'Add to cart'}</Button>
        </div>
      )}
    </>
  );
}
