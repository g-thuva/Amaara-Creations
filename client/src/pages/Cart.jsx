import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useStore } from '../contexts/StoreContext';
import { useResource } from '../hooks/useResource';
import { cartApi } from '../services/cartApi';
import { contentApi } from '../services/contentApi';
import { Button, ConfirmDialog, EmptyState, ErrorState, Icon, Media, PageHeader, Price, Quantity, Skeleton } from '../components/storefront/UI';

export default function Cart() {
  const { cart, pending, mutate } = useStore();
  const categories = useResource(contentApi.categories, 'cart-categories');
  const [confirm, setConfirm] = useState(null);
  const invalid = cart.data?.items.some((item) => item.isOutOfStock || item.quantity > item.productStock);

  const confirmRemoval = async () => {
    const action = () => confirm === 'all' ? cartApi.clearCart() : cartApi.removeFromCart(confirm.id);
    if (await mutate(action, cart.reload, 'Your cart has been updated.')) setConfirm(null);
  };

  return (
    <div className="s-container s-page">
      <PageHeader eyebrow="Your selection" title="Shopping cart" />
      {cart.loading ? <Skeleton count={2} /> : cart.error ? <ErrorState error={cart.error} retry={cart.reload} /> : !cart.data?.items.length ? (
        <EmptyState title="Your cart is ready for a favourite">
          <p>Add an available product from the shop to begin.</p>
          {categories.data?.length > 0 && (
            <div className="s-empty-categories">
              {categories.data.slice(0, 3).map((category) => <Link key={category.id} to={`/products?categoryId=${category.id}`}>{category.name}</Link>)}
            </div>
          )}
        </EmptyState>
      ) : (
        <div className="s-commerce-layout">
          <div>
            <div className="s-cart-items">
              {cart.data.items.map((item) => (
                <article className="s-cart-item" key={item.id}>
                  <Link className="s-cart-image" to={item.itemType === 'CustomDesign' ? item.editUrl : `/products/${item.productId}`}><Media src={item.productImageUrl} alt={item.productName} width={300} height={225} /></Link>
                  <div className="s-cart-details">
                    <h2><Link to={item.itemType === 'CustomDesign' ? item.editUrl : `/products/${item.productId}`}>{item.productName}</Link></h2>
                    {item.itemType === 'CustomDesign' && <p className="s-meta">Custom design · {item.width} × {item.height} {item.measurementUnit || 'cm'}{item.material ? ` · ${item.material}` : ''}{item.finish ? ` · ${item.finish}` : ''}</p>}
                    {item.itemType === 'CustomDesign' && item.customText && <p>Text: “{item.customText}”</p>}
                    <p><Price value={item.productPrice} /> each</p>
                    {(item.isOutOfStock || item.quantity > item.productStock) && (
                      <p className="s-error-text" role="alert">{item.isOutOfStock ? 'No longer available. Please remove this item.' : 'Availability changed. Please reduce the quantity.'}</p>
                    )}
                    <div className="s-cart-actions">
                      <Quantity label={`Quantity for ${item.productName}`} value={item.quantity} max={Math.max(item.productStock, 1)} disabled={pending || cart.loading || item.isOutOfStock} onChange={(quantity) => mutate(() => cartApi.updateCartItem(item.id, quantity), cart.reload)} />
                      <Button variant="text" disabled={pending} onClick={() => setConfirm(item)}>Remove</Button>
                    </div>
                  </div>
                  <div className="s-line-total"><span>Line subtotal</span><Price value={item.subtotal} /></div>
                </article>
              ))}
            </div>
            <div className="s-actions"><Link className="s-text-link" to="/products"><Icon name="arrowLeft" />Continue shopping</Link><Button variant="text" disabled={pending} onClick={() => setConfirm('all')}>Clear cart</Button></div>
          </div>
          <aside className="s-summary">
            <h2>Order summary</h2>
            <div className="s-row"><span>{cart.data.totalItems} items</span><Price value={cart.data.total} /></div>
            <strong className="s-summary-label">Subtotal</strong>
            <p>Shipping and payment arrangements are not included in this item subtotal.</p>
            {invalid ? <p className="s-error-text">Resolve unavailable items before continuing.</p> : <Link className="s-button s-button-primary" to="/checkout">Continue to checkout <Icon name="arrow" /></Link>}
          </aside>
        </div>
      )}
      <ConfirmDialog open={!!confirm} title={confirm === 'all' ? 'Clear your cart?' : 'Remove this item?'} pending={pending} onClose={() => setConfirm(null)} onConfirm={confirmRemoval}>
        {confirm === 'all' ? 'All items will be removed from your cart.' : confirm?.productName}
      </ConfirmDialog>
    </div>
  );
}
