import { Link } from 'react-router-dom';
import { useStore } from '../contexts/StoreContext';
import { wishlistApi } from '../services/wishlistApi';
import { Button, EmptyState, ErrorState, Icon, Media, PageHeader, Price, Skeleton } from '../components/storefront/UI';

export default function Wishlist() {
  const { wishlist, cart, pending, mutate } = useStore();
  return (
    <div className="s-container s-page">
      <PageHeader eyebrow="Saved for later" title="Your wishlist">Keep products here while you decide.</PageHeader>
      {wishlist.loading ? <Skeleton /> : wishlist.error ? <ErrorState error={wishlist.error} retry={wishlist.reload} /> : !wishlist.data?.items.length ? (
        <EmptyState title="Your wishlist is empty"><p>Use the heart button on a product to save it here.</p></EmptyState>
      ) : (
        <div className="s-product-grid s-wishlist-grid">
          {wishlist.data.items.map((item) => (
            <article className="s-product-card s-wishlist-card" key={item.id}>
              <Link className="s-wishlist-image" to={`/products/${item.productId}`}><Media src={item.productImageUrl} alt={item.productName} width={800} height={600} /></Link>
              <div className="s-product-info">
                {item.productCategory && <p className="s-meta">{item.productCategory}</p>}
                <h2><Link to={`/products/${item.productId}`}>{item.productName}</Link></h2>
                <Price value={item.productPrice} />
                <p className={`s-availability ${item.isOutOfStock ? 'unavailable' : 'available'}`}><Icon name={item.isOutOfStock ? 'x' : 'check'} />{item.isOutOfStock ? 'Out of stock' : 'In stock'}</p>
                <div className="s-stack">
                  <Button disabled={pending || item.isOutOfStock} onClick={() => mutate(() => wishlistApi.addWishlistItemToCart(item.productId), () => { cart.reload(); wishlist.reload(); }, 'Added to your cart.')}><Icon name="bag" />Move to cart</Button>
                  <Button variant="text" disabled={pending} onClick={() => mutate(() => wishlistApi.removeFromWishlist(item.productId), wishlist.reload, 'Removed from your wishlist.')}><Icon name="trash" />Remove</Button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
