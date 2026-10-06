import { Link } from 'react-router-dom';
import { useResource } from '../hooks/useResource';
import { contentApi } from '../services/contentApi';
import { productApi } from '../services/productApi';
import { useStore } from '../contexts/StoreContext';
import HeroCarousel from '../components/home/HeroCarousel';
import CategoryCards from '../components/home/CategoryCards';
import CollectionCards from '../components/home/CollectionCards';
import CustomCta from '../components/home/CustomCta';
import HowItWorks from '../components/home/HowItWorks';
import ProductCard from '../components/storefront/ProductCard';
import CmsSections from '../components/storefront/CmsSections';
import { ErrorState, Icon, SectionHeading, Skeleton } from '../components/storefront/UI';
import { safeLink } from '../utils/storefront';

function parseHero(section) {
  if (!section) return null;
  try {
    const value = JSON.parse(section.content);
    return value && typeof value === 'object' ? value : null;
  } catch {
    return null;
  }
}

export default function Home() {
  const { values } = useStore();
  const products = useResource(
    (signal) => productApi.getProducts({ pageSize: 8, sort: 'featured' }, signal),
    'home-products',
  );
  const categories = useResource(contentApi.categories, 'categories');
  const collections = useResource(contentApi.collections, 'collections');
  const content = useResource((signal) => contentApi.page('home', signal), 'home-content');
  const heroSection = content.data?.sections.find(
    (section) => section.isActive && section.sectionKey === 'hero' && section.contentType === 'json',
  );
  const hero = parseHero(heroSection);
  const featured = products.data?.products || [];
  const instagramUrl = safeLink(values['social.instagram']);
  const showInstagram = instagramUrl?.startsWith('https://');
  const instagramHandle = values['social.instagram.handle'] || '@amaara.creations';
  const trustItems = [values['delivery.note'], values['business.notice'], values['custom.notice']].filter(Boolean);

  return (
    <>
      <div className="s-container">
        <HeroCarousel hero={hero} />
      </div>

      {trustItems.length > 0 && (
        <section className="s-trust-strip" aria-label="Store information">
          <div className="s-container">
            {trustItems.map((item) => <span key={item}><Icon name="check" />{item}</span>)}
          </div>
        </section>
      )}

      <div className="s-container">
        {(categories.loading || categories.error || categories.data?.length > 0) && (
          <section className="s-section">
            <SectionHeading title="Our popular categories" to="/products" label="Shop all" />
            {categories.loading ? (
              <Skeleton count={3} />
            ) : categories.error ? (
              <ErrorState error={categories.error} retry={categories.reload} />
            ) : (
              <CategoryCards categories={categories.data} />
            )}
          </section>
        )}

        <CustomCta />

        <section className="s-section">
          <SectionHeading title="Featured from Amaara" to="/products" label="View all products" />
          {products.loading ? (
            <Skeleton count={8} />
          ) : products.error ? (
            <ErrorState error={products.error} retry={products.reload} />
          ) : featured.length ? (
            <div className="s-product-grid">
              {featured.map((product, index) => (
                <ProductCard key={product.id} product={product} eager={index < 4} />
              ))}
            </div>
          ) : (
            <div className="s-quiet-empty">
              <h3>Products are being prepared for the shop.</h3>
              <p>There are no published products yet. You can still preview a custom sticker idea.</p>
              <Link className="s-button s-button-secondary" to="/custom">Open the sticker preview <Icon name="arrow" /></Link>
            </div>
          )}
        </section>

        {collections.error && <ErrorState error={collections.error} retry={collections.reload} />}
        {collections.data?.length > 0 && (
          <section className="s-section s-section-tint">
            <SectionHeading title="Shop by collection" />
            <CollectionCards collections={collections.data} />
          </section>
        )}

        <section className="s-section">
          <SectionHeading title="How it works" />
          <HowItWorks />
        </section>

        {content.error && <ErrorState error={content.error} retry={content.reload} />}
        <CmsSections sections={content.data?.sections.filter((section) => section !== heroSection)} />
      </div>

      {showInstagram && (
        <section className="s-social-strip">
          <div className="s-container">
            <Icon name="instagram" />
            <div>
              <span>See recent Amaara work</span>
              <strong>Follow {instagramHandle}</strong>
            </div>
            <a className="s-button s-button-secondary" href={instagramUrl} target="_blank" rel="noopener noreferrer">
              Open Instagram <Icon name="arrow" />
            </a>
          </div>
        </section>
      )}
    </>
  );
}
