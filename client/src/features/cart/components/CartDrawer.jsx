import { Link } from 'react-router-dom';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { useCart } from '@/features/cart/CartProvider.jsx';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import CartArtwork from './CartArtwork.jsx';

export default function CartDrawer({ open, onClose }) {
  const { cart } = useCart(); const { isAuthenticated } = useAuth();
  const invalid = cart.data?.items.some(item => item.isOutOfStock || item.quantity > item.productStock) || !cart.data?.canCheckout;
  return <Dialog id="cart-preview" open={open} onClose={onClose} title="Cart preview" drawer>
    {!isAuthenticated ? <><p>Sign in to keep your cart across devices.</p><Link className="s-button s-button-primary" to="/cart" onClick={onClose}>Sign in to your cart</Link></> : cart.error ? <ErrorState error={cart.error} retry={cart.reload} /> : cart.loading ? <p role="status">Loading your cart…</p> : !cart.data?.items.length ? <><p>Your cart is ready for a favourite.</p><Link className="s-button s-button-primary" to="/products" onClick={onClose}>Explore products</Link></> : <>
      <p>{cart.data.totalItems} {cart.data.totalItems === 1 ? 'item' : 'items'}</p><ul className="s-cart-drawer-items">{cart.data.items.map(item => <li key={item.id}><Link to={item.editUrl || `/products/${item.productId}`} onClick={onClose}><CartArtwork item={item} /></Link><div><Link to={item.editUrl || `/products/${item.productId}`} onClick={onClose}>{item.productName}</Link><p>{item.quantity} × <Price value={item.productPrice} currency={item.currency} /></p>{item.itemType === 'CustomDesign' && <small>{item.width} × {item.height} {item.measurementUnit || 'cm'}{item.material ? ` · ${item.material}` : ''}</small>}</div></li>)}</ul>
      <div className="s-row"><strong>Item subtotal</strong>{cart.data.hasMixedCurrencies ? <span>Mixed currencies</span> : <Price value={cart.data.total} currency={cart.data.currency} />}</div><p className="s-meta">Shipping, discounts and payment arrangements are confirmed at checkout.</p><div className="s-actions"><Link className="s-button s-button-secondary" to="/cart" onClick={onClose}>View and edit cart</Link>{!invalid && <Link className="s-button s-button-primary" to="/checkout" onClick={onClose}>Continue to checkout</Link>}</div>
    </>}
  </Dialog>;
}
