import { Link } from 'react-router-dom';
import { Icon, Media } from '../storefront/UI';

export default function CategoryCards({ categories = [] }) {
  if (!categories.length) return null;
  return (
    <div className="s-category-grid">
      {categories.map((category) => (
        <article className="s-category-card" key={category.id}>
          <Link className="s-category-link" to={`/products?categoryId=${category.id}`} aria-label={`Shop ${category.name}`}>
            {category.imageUrl ? (
              <Media src={category.imageUrl} alt={category.name} width={800} height={600} />
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
      ))}
    </div>
  );
}

