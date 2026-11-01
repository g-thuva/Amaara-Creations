import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
import { Media } from '@/shared/components/ui/Media.jsx';

const igAssetPath = `${import.meta.env.BASE_URL}assets/instagram`;
const heroAssetPath = `${import.meta.env.BASE_URL}assets/hero`;

const categoryFallbackImages = {
  wedding: `${igAssetPath}/mirror-arch-sunset.jpg`,
  sign: `${igAssetPath}/tamil-wedding-sign.jpg`,
  sticker: `${heroAssetPath}/wedding-stationery.jpg`,
  decal: `${igAssetPath}/banana-leaf-sign.jpg`,
  event: `${igAssetPath}/mirror-arch-ceremony.jpg`,
  print: `${heroAssetPath}/luxury-welcome-sign-hero.jpg`,
};

function getCategoryFallback(name = '') {
  const lower = name.toLowerCase();
  for (const [key, path] of Object.entries(categoryFallbackImages)) {
    if (lower.includes(key)) return path;
  }
  return null;
}

export default function CategoryCards({ categories = [] }) {
  if (!categories.length) return null;
  return (
    <div className="s-category-grid">
      {categories.map((category) => {
        const image = category.imageUrl || getCategoryFallback(category.name);
        return (
          <article className="s-category-card" key={category.id}>
            <Link className="s-category-link" to={`/products?categoryId=${category.id}`}>
              {image ? (
                <Media src={image} alt={category.name} width={800} height={600} />
              ) : (
                <div className="s-category-fallback" aria-hidden="true">
                  <span>{category.name?.trim().charAt(0).toUpperCase() || 'A'}</span>
                </div>
              )}
              <span className="s-category-overlay" aria-hidden="true" />
              <div className="s-category-content">
                <h3>{category.name}</h3>
                {category.description && <p>{category.description}</p>}
                <span className="s-button s-button-outline-light">Shop now <Icon name="arrow" /></span>
              </div>
            </Link>
          </article>
        );
      })}
    </div>
  );
}
