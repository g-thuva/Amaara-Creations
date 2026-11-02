import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
import { Media } from '@/shared/components/ui/Media.jsx';

const igAssetPath = `${import.meta.env.BASE_URL}assets/instagram`;
const heroAssetPath = `${import.meta.env.BASE_URL}assets/hero`;

// Order matters: the first keyword found in the category name wins.
const categoryFallbackImages = {
  stationery: `${heroAssetPath}/wedding-stationery.jpg`,
  milestone: `${igAssetPath}/birthday-board.jpg`,
  wedding: `${heroAssetPath}/luxury-welcome-sign-hero.jpg`,
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
      {categories.map((category, index) => {
        const image = category.imageUrl || getCategoryFallback(category.name);
        return (
          <article className="s-category-card" key={category.id}>
            <Link className="s-category-link" to={`/products?categoryId=${category.id}`}>
              <div className="s-category-media">
                {image ? (
                  <Media src={image} alt={category.name} width={800} height={600} />
                ) : (
                  <div className="s-category-fallback" aria-hidden="true">
                    <span>{category.name?.trim().charAt(0).toUpperCase() || 'A'}</span>
                  </div>
                )}
                <span className="s-category-index" aria-hidden="true">{String(index + 1).padStart(2, '0')}</span>
              </div>
              <div className="s-category-content">
                <h3>{category.name}</h3>
                {category.description && <p>{category.description}</p>}
                <span className="s-category-cta">
                  Shop now <Icon name="arrow" />
                </span>
              </div>
            </Link>
          </article>
        );
      })}
    </div>
  );
}
