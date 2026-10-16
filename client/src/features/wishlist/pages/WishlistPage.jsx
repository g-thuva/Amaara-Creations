import { useWishlistPage } from '@/features/wishlist/hooks/useWishlistPage.js';
import WishlistItem from '@/features/wishlist/components/WishlistItem.jsx';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';

export default function WishlistPage() {
  const { wishlist, pending, moveToCart, remove } = useWishlistPage();
  return (
    <div className="s-container s-page">
      <PageHeader eyebrow="Saved for later" title="Your wishlist">Keep products here while you decide.</PageHeader>
      {wishlist.loading ? <Skeleton /> : wishlist.error ? <ErrorState error={wishlist.error} retry={wishlist.reload} /> : !wishlist.data?.items.length ? (
        <EmptyState to="/products" label="Explore products" title="Your wishlist is empty"><p>Use the heart button on a product to save it here.</p></EmptyState>
      ) : (
        <div className="s-product-grid s-wishlist-grid">
          {wishlist.data.items.map((item) => (
            <WishlistItem key={item.id} item={item} pending={pending} moveToCart={moveToCart} remove={remove} />
          ))}
        </div>
      )}
    </div>
  );
}
