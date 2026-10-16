import CheckoutForm from '@/features/checkout/components/CheckoutForm.jsx';
import CheckoutReview from '@/features/checkout/components/CheckoutReview.jsx';
import { useCart } from '@/features/cart/CartProvider.jsx';
import { useCheckout } from '@/features/checkout/hooks/useCheckout.js';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';

export default function CheckoutPage() {
  const { cart } = useCart();
  const checkout = useCheckout(JSON.stringify(cart.data?.items.map(i => [i.id, i.quantity, i.updatedAt]) || []));
  const { addresses, quote } = checkout;
  return <div className="s-container s-page">
    <PageHeader eyebrow="Secure checkout" title="Checkout">Choose your address and delivery option, review the server total, then continue to secure payment.</PageHeader>
    {cart.loading || addresses.loading ? <Skeleton count={2} /> : cart.error ? <ErrorState error={cart.error} retry={cart.reload} /> : addresses.error ? <ErrorState error={addresses.error} retry={addresses.reload} /> : !cart.data?.items.length ? <EmptyState to="/products" label="Explore products" title="Your cart is empty" /> : !addresses.data?.length ? <EmptyState title="Add a delivery address" to="/addresses" label="Manage addresses"><p>Save an address, then return to checkout.</p></EmptyState> : <div className="s-commerce-layout">
      <CheckoutForm checkout={checkout} />
      <CheckoutReview quote={quote} />
    </div>}
  </div>;
}
