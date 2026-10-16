import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { Media } from '@/shared/components/ui/Media.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
export default function WishlistItem({ item, pending, moveToCart, remove }) {
  return (
            <article className="s-product-card s-wishlist-card" key={item.id}>
              <Link className="s-wishlist-image" to={`/products/${item.productId}`}><Media src={item.productImageUrl} alt={item.productName} width={800} height={600} /></Link>
              <div className="s-product-info">
                {item.productCategory && <p className="s-meta">{item.productCategory}</p>}
                <h2><Link to={`/products/${item.productId}`}>{item.productName}</Link></h2>
                <Price value={item.productPrice} currency={item.currency} />
                <p className={`s-availability ${item.isOutOfStock ? 'unavailable' : 'available'}`}><Icon name={item.isOutOfStock ? 'x' : 'check'} />{item.isOutOfStock ? 'Out of stock' : 'In stock'}</p>
                <div className="s-stack">
                  <Button disabled={pending || item.isOutOfStock} onClick={() => moveToCart(item.productId)}><Icon name="bag" />Move to cart</Button>
                  <Button variant="text" disabled={pending} onClick={() => remove(item.productId)}><Icon name="trash" />Remove</Button>
                </div>
              </div>
            </article>
  );
}
