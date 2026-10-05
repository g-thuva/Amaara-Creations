import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useResource } from '../hooks/useResource';
import { productApi } from '../services/productApi';
import { cartApi } from '../services/cartApi';
import { useStore } from '../contexts/StoreContext';
import { Button, EmptyState, ErrorState, Media, Price, Quantity, Skeleton } from '../components/storefront/UI';
import Reviews from '../components/storefront/Reviews';
function Product({ product }) {
  const [selected, setSelected] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [image, setImage] = useState(0);
  const { cart, wishlist, pending, mutate, toggleWishlist } = useStore();
  const variant = product.variants?.find(v => String(v.id) === selected);
  const stock = variant ? variant.stockQuantity : product.stock;
  const gallery = product.media?.length ? product.media : [{ url: product.imageUrl, altText: product.name }];
  const saved = wishlist.data?.items.some(i => i.productId === product.id);
  return <>
    <nav className="s-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span>/</span><Link to="/products">Shop</Link><span>/</span><span aria-current="page">{product.name}</span></nav>
    <div className="s-pdp"><section aria-label="Product gallery"><Media eager src={gallery[image]?.url} alt={gallery[image]?.altText || product.name}/>{gallery.length > 1 && <div className="s-thumbnails">{gallery.map((media, index) => <button key={media.id || index} aria-label={`View image ${index + 1}`} aria-pressed={index === image} onClick={() => setImage(index)}><Media src={media.url} alt={media.altText || `${product.name}, image ${index + 1}`}/></button>)}</div>}</section>
      <section className="s-pdp-info"><p className="s-eyebrow">{product.category}</p><h1>{product.name}</h1><Price value={variant?.priceOverride ?? product.price}/><p className="s-lead">{product.shortDescription || product.description}</p><p className={`s-availability ${stock > 0 ? 'available' : ''}`}>{stock > 0 ? 'In stock' : 'Out of stock'}</p>
        {product.variants?.length > 0 && <label className="s-field">Product option<select value={selected} onChange={e => { setSelected(e.target.value); setQuantity(1); }}><option value="">Standard product</option>{product.variants.map(v => <option key={v.id} value={v.id} disabled={v.stockQuantity <= 0}>{v.name}{v.stockQuantity <= 0 ? ' — out of stock' : ''}</option>)}</select></label>}
        {variant && <p className="s-alert">This option is available to view. Online ordering for selected options is not available yet. <Link to="/contact">Contact us about this option.</Link></p>}
        <div className="s-purchase"><Quantity value={quantity} onChange={setQuantity} max={stock} disabled={pending || stock <= 0}/><Button disabled={pending || stock <= 0 || !!variant} onClick={() => mutate(() => cartApi.addToCart(product.id, quantity), cart.reload, 'Added to your cart.')}>{pending ? 'Please wait…' : stock <= 0 ? 'Out of stock' : 'Add to cart'}</Button></div>
        <Button variant="text" aria-pressed={!!saved} disabled={pending || wishlist.loading} onClick={() => toggleWishlist(product.id)}>{saved ? '♥ Saved to wishlist' : '♡ Save to wishlist'}</Button>
        <details className="s-product-description" open><summary>About this product</summary><p>{product.description || 'No additional description is available.'}</p></details>
      </section>
    </div><Reviews productId={product.id}/>
  </>;
}
export default function ProductDetails() {
  const { id } = useParams();
  const resource = useResource(signal => productApi.getProductById(id, signal), id);
  return <div className="s-container s-page">{resource.loading ? <Skeleton count={2}/> : resource.error?.response?.status === 404 ? <EmptyState title="Product not found">This product may no longer be available.</EmptyState> : resource.error ? <ErrorState error={resource.error} retry={resource.reload}/> : <Product key={id} product={resource.data}/>}</div>;
}
