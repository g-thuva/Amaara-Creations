import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import CartArtwork from './CartArtwork.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
import { Quantity } from '@/shared/components/ui/Quantity.jsx';
export default function CartItem({ item, pending, loading, updateQuantity, remove }) {
  return (
                <article className="s-cart-item" key={item.id}>
                  <Link className="s-cart-image" to={item.itemType === 'CustomDesign' ? item.editUrl : `/products/${item.productId}`}><CartArtwork item={item} /></Link>
                  <div className="s-cart-details">
                    <h2><Link to={item.itemType === 'CustomDesign' ? item.editUrl : `/products/${item.productId}`}>{item.productName}</Link></h2>
                    {item.itemType === 'CustomDesign' && <p className="s-meta">Custom design · {item.width} × {item.height} {item.measurementUnit || 'cm'}{item.material ? ` · ${item.material}` : ''}{item.finish ? ` · ${item.finish}` : ''}</p>}
                    {item.itemType === 'CustomDesign' && item.customText && <p>Text: “{item.customText}”</p>}
                    <p><Price value={item.productPrice} currency={item.currency} /> each</p>
                    {(item.isOutOfStock || item.quantity > item.productStock) && (
                      <p className="s-error-text" role="alert">{item.isOutOfStock ? 'No longer available. Please remove this item.' : 'Availability changed. Please reduce the quantity.'}</p>
                    )}
                    <div className="s-cart-actions">
                      <Quantity label={`Quantity for ${item.productName}`} value={item.quantity} max={Math.max(item.productStock, 1)} disabled={pending || loading || item.isOutOfStock} onChange={(quantity) => updateQuantity(item.id, quantity)} />
                      <Button variant="text" disabled={pending} onClick={() => remove(item)}>Remove</Button>
                    </div>
                  </div>
                  <div className="s-line-total"><span>Line subtotal</span><Price value={item.subtotal} currency={item.currency} /></div>
                </article>
  );
}
