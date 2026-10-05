import { Link } from 'react-router-dom';
import { useResource } from '../hooks/useResource';
import { contentApi } from '../services/contentApi';
import { productApi } from '../services/productApi';
import ProductCard from '../components/storefront/ProductCard';
import CmsSections from '../components/storefront/CmsSections';
import { ErrorState, Icon, Media, SectionHeading, Skeleton } from '../components/storefront/UI';
import { safeLink } from '../utils/storefront';
export default function Home() {
  const products = useResource(signal => productApi.getProducts({ pageSize: 4, sort: 'featured' }, signal), 'home-products');
  const categories = useResource(contentApi.categories, 'categories');
  const collections = useResource(contentApi.collections, 'collections');
  const content = useResource(signal => contentApi.page('home', signal), 'home-content');
  const heroSection = content.data?.sections.find(s => s.isActive && s.sectionKey === 'hero' && s.contentType === 'json');
  let hero = {};
  try { hero = heroSection ? JSON.parse(heroSection.content) || {} : {}; } catch { /* optional block uses brand fallback */ }
  const text = (value, fallback) => typeof value === 'string' && value ? value : fallback;
  const featured = products.data?.products || [];
  const heroImage = typeof hero.imageUrl === 'string' ? hero.imageUrl : featured.find(p => p.imageUrl)?.imageUrl;
  const heroCta = safeLink(hero.ctaUrl);
  return <>
    <section className="s-hero"><div className="s-container s-hero-grid"><div className="s-hero-copy"><p className="s-eyebrow">A little detail. All your own.</p><h1>{text(hero.title, 'Make everyday things feel like you.')}</h1><p className="s-lead">{text(hero.body, 'Discover stickers, thoughtful prints and creative details. Find your favourites, or explore an idea of your own.')}</p>
      <div className="s-actions">{heroCta && !heroCta.startsWith('/') ? <a className="s-button s-button-primary" href={heroCta}>{text(hero.ctaLabel, 'Explore the shop')}<Icon name="arrow"/></a> : <Link className="s-button s-button-primary" to={heroCta || '/products'}>{text(hero.ctaLabel, 'Explore the shop')}<Icon name="arrow"/></Link>}<Link className="s-text-link" to="/custom">Try a custom idea ↗</Link></div>
    </div><div className="s-hero-art">{heroImage ? <Media eager src={heroImage} alt={text(hero.alt, 'Discover Amaara creations')}/> : <div className="s-type-art" aria-hidden="true"><span className="s-art-caption">AMAARA / CREATIVE STUDIO</span><span className="s-art-star">✳</span><span className="s-art-label">small details,<br/><em>big personality.</em></span><span className="s-art-bottom">PRINT · STICK · MAKE IT YOURS</span></div>}</div></div></section>
    <div className="s-container">
      {(categories.loading || categories.error || categories.data?.length > 0) && <section className="s-section"><SectionHeading title="Find your kind of creative" to="/products" label="All products"/>{categories.loading ? <Skeleton count={3}/> : categories.error ? <ErrorState error={categories.error} retry={categories.reload}/> : <div className="s-category-grid">{categories.data.map((category, index) => <Link key={category.id} className="s-category" to={`/products?categoryId=${category.id}`}>{category.imageUrl && <Media src={category.imageUrl} alt={category.name}/>}<span className="s-meta">{String(index + 1).padStart(2, '0')}</span><h3>{category.name}</h3>{category.description && <p>{category.description}</p>}<Icon name="arrow"/></Link>)}</div>}</section>}
      <section className="s-custom-banner"><div><p className="s-eyebrow">Your idea starts here</p><h2>A sticker with<br/>your name on it.</h2></div><div><p>Play with type and dimensions in our sticker preview. A small space for your next big idea.</p><Link className="s-button s-button-secondary" to="/custom">Explore custom stickers<Icon name="arrow"/></Link></div></section>
      <section className="s-section"><SectionHeading title="From the studio" to="/products" label="Shop the collection"/>{products.loading ? <Skeleton/> : products.error ? <ErrorState error={products.error} retry={products.reload}/> : featured.length ? <div className="s-product-grid">{featured.map(product => <ProductCard key={product.id} product={product}/>)}</div> : <div className="s-quiet-empty"><h3>New creations are on their way.</h3><p>There are no products available just yet. You can still explore a custom sticker idea.</p><Link to="/custom">Open the sticker preview ↗</Link></div>}</section>
      {collections.error && <ErrorState error={collections.error} retry={collections.reload}/>}{collections.data?.length > 0 && <section className="s-section"><SectionHeading title="Collected for you"/><div className="s-collection-list">{collections.data.map(c => <Link key={c.id} to={`/products?collectionId=${c.id}`}><div><h3>{c.name}</h3>{c.description && <p>{c.description}</p>}</div><span>{c.productCount} products ↗</span></Link>)}</div></section>}
      <section className="s-section s-how"><div><p className="s-eyebrow">A little inspiration</p><h2>From an idea<br/>to your favourites.</h2></div><ol><li><span>01</span><div><h3>Find your thing</h3><p>Browse products and collections to discover a detail you love.</p></div></li><li><span>02</span><div><h3>Make a shortlist</h3><p>Save favourites to your wishlist, or preview your own sticker text.</p></div></li><li><span>03</span><div><h3>Take a closer look</h3><p>Check product details and availability before adding to your cart.</p></div></li></ol></section>
      {content.error && <ErrorState error={content.error} retry={content.reload}/>}<CmsSections sections={content.data?.sections.filter(s => s !== heroSection)}/>
    </div>
  </>;
}
