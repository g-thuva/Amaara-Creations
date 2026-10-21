import { readCmsObject } from '@/features/content/contentModel.js';
import { useSiteContent } from '@/features/content/SiteContentProvider.jsx';
import { Link } from 'react-router-dom';
import { useResource } from '@/shared/hooks/useResource.js';
import { contentApi } from '@/features/content/contentApi.js';
import { catalogApi } from '@/features/catalog/catalogApi.js';
import HeroCarousel from '@/features/home/components/HeroCarousel.jsx';
import EditorialHero from '@/features/home/components/EditorialHero.jsx';
import CategoryCards from '@/features/home/components/CategoryCards.jsx';
import CollectionCards from '@/features/home/components/CollectionCards.jsx';
import CustomCta from '@/features/home/components/CustomCta.jsx';
import HowItWorks from '@/features/home/components/HowItWorks.jsx';
import ProductCard from '@/features/catalog/components/ProductCard.jsx';
import CmsSections from '@/features/content/components/CmsSections.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { SectionHeading } from '@/shared/components/ui/SectionHeading.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import { safeLink } from '@/shared/utils/safeLink.ts';

export default function HomePage() {
  const { values } = useSiteContent();
  const products = useResource(
    (signal) => catalogApi.getProducts({ pageSize: 8, sort: 'featured' }, signal),
    'home-products',
  );
  const categories = useResource(catalogApi.getCategories, 'categories');
  const collections = useResource(catalogApi.getCollections, 'collections');
  const content = useResource((signal) => contentApi.page('home', signal), 'home-content');
  const heroSection = content.data?.sections.find(
    (section) => section.isActive && section.sectionKey === 'hero' && section.contentType === 'json',
  );
  const hero = readCmsObject(heroSection?.content);
  const featured = products.data?.products || [];
  const instagramUrl = safeLink(values['social.instagram']);
  const showInstagram = instagramUrl?.startsWith('https://');
  const instagramHandle = values['social.instagram.handle'] || '@amaara.creations';
  const trustItems = [values['delivery.note'], values['business.notice'], values['custom.notice']].filter(Boolean);

  return (
    <>
      <div className="s-container">
        {hero ? <HeroCarousel hero={hero} /> : <EditorialHero />}
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
              <p className="s-eyebrow">Start with your own idea</p>
              <h3>Your next design starts here.</h3>
              <p>The ready-made collection is currently empty. Explore the sticker designer to see available shapes and options.</p>
              <Link className="s-button s-button-secondary" to="/custom">Design your sticker <Icon name="arrow" /></Link>
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
