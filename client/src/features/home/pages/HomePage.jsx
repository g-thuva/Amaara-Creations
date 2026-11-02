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
import InstagramShowcase from '@/features/home/components/InstagramShowcase.jsx';
import FeaturedVideos from '@/features/home/components/FeaturedVideos.jsx';
import CmsSections from '@/features/content/components/CmsSections.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { SectionHeading } from '@/shared/components/ui/SectionHeading.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';

export default function HomePage() {
  const { values } = useSiteContent();
  const categories = useResource(catalogApi.getCategories, 'categories');
  const collections = useResource(catalogApi.getCollections, 'collections');
  const content = useResource((signal) => contentApi.page('home', signal), 'home-content');
  const heroSection = content.data?.sections.find(
    (section) => section.isActive && section.sectionKey === 'hero' && section.contentType === 'json',
  );
  const hero = readCmsObject(heroSection?.content);
  const trustItems = [values['delivery.note'], values['business.notice'], values['custom.notice']].filter(Boolean);

  return (
    <>
      {hero ? (
        <div className="s-container">
          <HeroCarousel hero={hero} />
        </div>
      ) : (
        <EditorialHero />
      )}

      {trustItems.length > 0 && (
        <section className="s-trust-strip" aria-label="Store information">
          <div className="s-container">
            {trustItems.map((item) => <span key={item}><Icon name="check" />{item}</span>)}
          </div>
        </section>
      )}

      <div className="s-container">
        {/* Real Creations & Instagram Portfolio Spotlight */}
        <InstagramShowcase />
      </div>

      <CustomCta />

      <div className="s-container">
        <section className="s-section">
          <SectionHeading title={<>Featured designs <em>entirely yours</em></>} to="/products" label="View all products" align="left" />
          <FeaturedVideos />
        </section>

        {collections.error && <ErrorState error={collections.error} retry={collections.reload} />}
        {collections.data?.length > 0 && (
          <section className="s-section s-section-tint">
            <SectionHeading title={<>Shop by <em>collection</em></>} />
            <CollectionCards collections={collections.data} />
          </section>
        )}

        <HowItWorks />

        {(categories.loading || categories.error || categories.data?.length > 0) && (
          <section className="s-section" aria-labelledby="shop-by-category">
            <SectionHeading title={<span id="shop-by-category">Shop by <em>category</em></span>} to="/products" label="Shop all" />
            {categories.loading ? (
              <Skeleton count={3} />
            ) : categories.error ? (
              <ErrorState error={categories.error} retry={categories.reload} />
            ) : (
              <CategoryCards categories={categories.data} />
            )}
          </section>
        )}

        {content.error && <ErrorState error={content.error} retry={content.reload} />}
        <CmsSections sections={content.data?.sections.filter((section) => section !== heroSection)} />
      </div>
    </>
  );
}
