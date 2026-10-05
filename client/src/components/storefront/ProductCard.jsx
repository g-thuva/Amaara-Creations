import { Link } from 'react-router-dom';
import { useStore } from '../../contexts/StoreContext';
import { Icon, Media, Price } from './UI';
export default function ProductCard({ product }) {
  const { wishlist, toggleWishlist, pending } = useStore();
  const saved = !!wishlist.data?.items.some(item => item.productId === product.id);
  return <article className="s-product-card">
    <div className="s-product-visual"><Link to={`/products/${product.id}`} aria-label={`View ${product.name}`}><Media src={product.imageUrl} alt={product.name}/></Link>
      <button className="s-save" aria-label={`${saved ? 'Remove' : 'Save'} ${product.name} ${saved ? 'from' : 'to'} wishlist`} aria-pressed={saved} disabled={pending || wishlist.loading} onClick={() => toggleWishlist(product.id)}><Icon name="heart" fill={saved ? 'currentColor' : 'none'}/></button>
      {product.stock <= 0 && <span className="s-stock-label">Out of stock</span>}
    </div>
    <div className="s-product-info"><p className="s-meta">{product.category}</p><h3><Link to={`/products/${product.id}`}>{product.name}</Link></h3><Price value={product.price}/></div>
  </article>;
}
