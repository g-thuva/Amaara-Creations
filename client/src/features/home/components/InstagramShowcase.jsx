import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';

const igAssetPath = `${import.meta.env.BASE_URL}assets/instagram`;
const productAssetPath = `${import.meta.env.BASE_URL}assets/products`;

// Sample gallery items. These will later be fetched from the backend.
const galleryItems = [
  {
    id: 'g1',
    title: 'Sunset Mirror Arch',
    caption: 'Arched mirror welcome sign glowing at golden hour.',
    image: `${igAssetPath}/mirror-arch-sunset.jpg`,
  },
  {
    id: 'g2',
    title: 'Kalyanam Gold Calligraphy',
    caption: 'Traditional Tamil ceremony sign in hand-lettered gold.',
    image: `${igAssetPath}/tamil-wedding-sign.jpg`,
  },
  {
    id: 'g3',
    title: 'Banana Leaf Ritual Board',
    caption: 'Frosted acrylic board with an auspicious thali motif.',
    image: `${igAssetPath}/banana-leaf-sign.jpg`,
  },
  {
    id: 'g4',
    title: 'Happily Ever After',
    caption: 'Mirror arch welcome sign framed with fresh florals.',
    image: `${igAssetPath}/mirror-arch-ceremony.jpg`,
  },
  {
    id: 'g5',
    title: 'She Said Yes!',
    caption: 'Frosted acrylic proposal and engagement sign.',
    image: `${igAssetPath}/she-said-yes-sign.jpg`,
  },
  {
    id: 'g6',
    title: 'Couple Portrait Board',
    caption: 'Personalised illustrated welcome board for the couple.',
    image: `${igAssetPath}/kalyanam-welcome-board.jpg`,
  },
  {
    id: 'g7',
    title: 'Milestone Birthday Board',
    caption: 'Hand-illustrated cartoon portrait for a 30th celebration.',
    image: `${igAssetPath}/birthday-board.jpg`,
  },
  {
    id: 'g8',
    title: 'This Way Direction Sign',
    caption: 'Guide your guests with an elegant arrow sign.',
    image: `${productAssetPath}/wedding-direction-arrow.jpg`,
  },
  {
    id: 'g9',
    title: 'Table Numbers & Seating',
    caption: 'Minimal table cards to match your wedding stationery.',
    image: `${productAssetPath}/table-numbers-stationery.jpg`,
  },
];

function GallerySlide({ item, hidden = false }) {
  return (
    <li className="s-gallery-slide" aria-hidden={hidden || undefined}>
      <Link to="/products" tabIndex={hidden ? -1 : undefined} className="s-gallery-link">
        <div className="s-gallery-image">
          <img src={item.image} alt={hidden ? '' : item.title} loading="lazy" decoding="async" width="400" height="400" />
        </div>
        <div className="s-gallery-caption">
          <h3>{item.title}</h3>
          <p>{item.caption}</p>
        </div>
      </Link>
    </li>
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

      <div className="s-gallery-marquee">
        <ul className="s-gallery-track">
          {galleryItems.map((item) => <GallerySlide key={item.id} item={item} />)}
          {/* Duplicate set for a seamless infinite loop */}
          {galleryItems.map((item) => <GallerySlide key={`${item.id}-dup`} item={item} hidden />)}
        </ul>
      </div>
    </section>
  );
}
