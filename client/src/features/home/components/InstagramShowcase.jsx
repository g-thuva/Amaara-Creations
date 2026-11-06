import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';

const igAssetPath = `${import.meta.env.BASE_URL}assets/instagram`;
const productAssetPath = `${import.meta.env.BASE_URL}assets/products`;

// Sample gallery items. These will later be fetched from the backend.
const galleryItems = [
  {
    id: 'g1',
    tag: 'Mirror Arch',
    title: 'Sunset Mirror Arch',
    caption: 'Arched mirror welcome sign glowing at golden hour.',
    image: `${igAssetPath}/mirror-arch-sunset.jpg`,
  },
  {
    id: 'g2',
    tag: 'Gold Calligraphy',
    title: 'Kalyanam Gold Calligraphy',
    caption: 'Traditional Tamil ceremony sign in hand-lettered gold.',
    image: `${igAssetPath}/tamil-wedding-sign.jpg`,
  },
  {
    id: 'g3',
    tag: 'Frosted Acrylic',
    title: 'Banana Leaf Ritual Board',
    caption: 'Frosted acrylic board with an auspicious thali motif.',
    image: `${igAssetPath}/banana-leaf-sign.jpg`,
  },
  {
    id: 'g4',
    tag: 'Floral Welcome',
    title: 'Happily Ever After',
    caption: 'Mirror arch welcome sign framed with fresh florals.',
    image: `${igAssetPath}/mirror-arch-ceremony.jpg`,
  },
  {
    id: 'g5',
    tag: 'Engagement Sign',
    title: 'She Said Yes!',
    caption: 'Frosted acrylic proposal and engagement sign.',
    image: `${igAssetPath}/she-said-yes-sign.jpg`,
  },
  {
    id: 'g6',
    tag: 'Custom Portrait',
    title: 'Couple Portrait Board',
    caption: 'Personalised illustrated welcome board for the couple.',
    image: `${igAssetPath}/kalyanam-welcome-board.jpg`,
  },
  {
    id: 'g7',
    tag: 'Milestone Event',
    title: 'Milestone Birthday Board',
    caption: 'Hand-illustrated portrait sign for a 30th celebration.',
    image: `${igAssetPath}/birthday-board.jpg`,
  },
  {
    id: 'g8',
    tag: 'Ceremony Wayfinding',
    title: 'This Way Direction Sign',
    caption: 'Guide your guests with an elegant bespoke arrow sign.',
    image: `${productAssetPath}/wedding-direction-arrow.jpg`,
  },
];

const AUTOPLAY_MS = 5500;

function useItemsPerView() {
  const read = () => {
    if (typeof window === 'undefined') return 4;
    if (window.matchMedia('(max-width: 599px)').matches) return 1;
    if (window.matchMedia('(max-width: 959px)').matches) return 2;
    return 4;
  };
  const [perView, setPerView] = useState(read);
  useEffect(() => {
    const onChange = () => setPerView(read());
    const queries = ['(max-width: 599px)', '(max-width: 959px)'].map((q) => window.matchMedia(q));
    queries.forEach((q) => q.addEventListener('change', onChange));
    return () => queries.forEach((q) => q.removeEventListener('change', onChange));
  }, []);
  return perView;
}

function GallerySlide({ item, inactive = false }) {
  return (
    <li className="s-gallery-slide">
      <Link to="/products" tabIndex={inactive ? -1 : undefined} className="s-gallery-link">
        <div className="s-gallery-image">
          <img src={item.image} alt={item.title} loading="lazy" decoding="async" width="400" height="500" />
        </div>
        <div className="s-gallery-caption">
          <div className="s-gallery-meta">
            <span className="s-gallery-tag">{item.tag}</span>
          </div>
          <h3>{item.title}</h3>
          <p>{item.caption}</p>
        </div>
      </Link>
    </li>
  );
}

function GalleryCarousel() {
  const perView = useItemsPerView();
  const pages = [];
  for (let i = 0; i < galleryItems.length; i += perView) pages.push(galleryItems.slice(i, i + perView));
  const [page, setPage] = useState(0);
  const [paused, setPaused] = useState(false);
  const current = Math.min(page, pages.length - 1);

  const go = useCallback((next) => setPage((next + pages.length) % pages.length), [pages.length]);

  useEffect(() => {
    if (paused || pages.length < 2) return undefined;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return undefined;
    const timer = setTimeout(() => go(current + 1), AUTOPLAY_MS);
    return () => clearTimeout(timer);
  }, [current, paused, pages.length, go]);

  return (
    <div
      className="s-gallery-carousel"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocusCapture={() => setPaused(true)}
      onBlurCapture={() => setPaused(false)}
    >
      <div className="s-gallery-viewport">
        <div className="s-gallery-track" style={{ '--page': current }}>
          {pages.map((items, pageIndex) => (
            <ul
              className="s-gallery-page"
              key={pageIndex}
              style={{ '--per-view': perView }}
              aria-hidden={pageIndex !== current || undefined}
              aria-label={`Gallery page ${pageIndex + 1} of ${pages.length}`}
            >
              {items.map((item) => (
                <GallerySlide
                  key={item.id}
                  item={item}
                  inactive={pageIndex !== current}
                />
              ))}
            </ul>
          ))}
        </div>
      </div>

      {pages.length > 1 && (
        <div className="s-gallery-controls">
          <button type="button" className="s-gallery-arrow" aria-label="Previous creations" onClick={() => go(current - 1)}>
            <Icon name="arrowLeft" />
          </button>
          <div className="s-gallery-dots" role="group" aria-label="Choose gallery page">
            {pages.map((_, index) => (
              <button
                key={index}
                type="button"
                className={index === current ? 'is-active' : ''}
                aria-label={`Show creations page ${index + 1}`}
                aria-current={index === current || undefined}
                onClick={() => go(index)}
              />
            ))}
          </div>
          <button type="button" className="s-gallery-arrow" aria-label="Next creations" onClick={() => go(current + 1)}>
            <Icon name="arrow" />
          </button>
        </div>
      )}
    </div>
  );
}

export default function InstagramShowcase() {
  return (
    <section className="s-section s-ig-showcase" aria-labelledby="ig-showcase-title">
      <div className="s-ig-header">
        <div className="s-ig-header-copy">
          <p className="s-eyebrow">
            <span className="s-eyebrow-accent" aria-hidden="true" />
            The Bespoke Collection
          </p>
          <h2 id="ig-showcase-title">
            Crafted for life&rsquo;s <em>most beautiful moments</em>
          </h2>
          <p className="s-ig-subtitle">
            Bespoke acrylic welcome signs, mirror arches, direction signs and stationery handcrafted for life&rsquo;s most beautiful celebrations.
          </p>
        </div>
        <Link className="s-ig-follow-pill" to="/products">
          <span>Explore all wedding signs</span>
          <Icon name="arrow" />
        </Link>
      </div>

      <GalleryCarousel />
    </section>
  );
}
