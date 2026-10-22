import CartItem from '@/features/cart/components/CartItem.jsx';
import CartSummary from '@/features/cart/components/CartSummary.jsx';
import { useCartPage } from '@/features/cart/hooks/useCartPage.js';
import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog.jsx';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';

export default function CartPage() {
  const { cart, pending, categories, confirm, setConfirm, invalid, currencyMismatch, confirmRemoval, updateQuantity, lastRemoved, undoRemoval } = useCartPage();
  return (
    <div className="s-container s-page">
      <PageHeader eyebrow="Your selection" title="Shopping cart" />
      {lastRemoved && <div className="s-alert" role="status"><p>{lastRemoved.productName} was removed. Restore uses current availability and pricing.</p><Button variant="secondary" disabled={pending} onClick={undoRemoval}>Restore removed item</Button></div>}
      {cart.loading ? <Skeleton count={2} /> : cart.error ? <ErrorState error={cart.error} retry={cart.reload} /> : !cart.data?.items.length ? (
        <EmptyState to="/products" label="Explore products" title="Your cart is ready for a favourite">
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
                <CartItem key={item.id} item={item} pending={pending} loading={cart.loading} updateQuantity={updateQuantity} remove={setConfirm} />
              ))}
            </div>
            <div className="s-actions"><Link className="s-text-link" to="/products"><Icon name="arrowLeft" />Continue shopping</Link><Button variant="text" disabled={pending} onClick={() => setConfirm('all')}>Clear cart</Button></div>
          </div>
          <CartSummary cart={cart} invalid={invalid} currencyMismatch={currencyMismatch} />
        </div>
      )}
      <ConfirmDialog open={!!confirm} title={confirm === 'all' ? 'Clear your cart?' : 'Remove this item?'} pending={pending} onClose={() => setConfirm(null)} onConfirm={confirmRemoval}>
        {confirm === 'all' ? 'All items will be removed from your cart.' : confirm?.productName}
      </ConfirmDialog>
    </div>
  );
}
