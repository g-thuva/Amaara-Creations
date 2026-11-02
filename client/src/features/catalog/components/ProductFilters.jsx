import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';

export default function ProductFilters({ params, categories, collections, apply, clear, currency }) {
  const hasActiveFilters = Boolean(
    params.get('categoryId') ||
    params.get('collectionId') ||
    params.get('minPrice') ||
    params.get('maxPrice') ||
    params.get('inStock')
  );

  return (
    <form className="s-filter-form" onSubmit={(event) => {
      event.preventDefault();
      apply(new FormData(event.currentTarget));
    }}>
      {/* Category */}
      <div className="s-filter-group">
        <label className="s-filter-label" htmlFor="filter-category">
          <span>Category</span>
        </label>
        <div className="s-filter-select-wrap">
          <select id="filter-category" name="categoryId" defaultValue={params.get('categoryId') || ''}>
            <option value="">All categories</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>{category.name}</option>
            ))}
          </select>
          <Icon name="chevronDown" className="s-filter-select-arrow" />
        </div>
      </div>

      {/* Collection */}
      <div className="s-filter-group">
        <label className="s-filter-label" htmlFor="filter-collection">
          <span>Collection</span>
        </label>
        <div className="s-filter-select-wrap">
          <select id="filter-collection" name="collectionId" defaultValue={params.get('collectionId') || ''}>
            <option value="">All collections</option>
            {collections.map((collection) => (
              <option key={collection.id} value={collection.id}>{collection.name}</option>
            ))}
          </select>
          <Icon name="chevronDown" className="s-filter-select-arrow" />
        </div>
      </div>

      {/* Price range */}
      <fieldset className="s-filter-group s-filter-fieldset">
        <legend className="s-filter-label">Price range ({currency})</legend>
        <div className="s-price-range-grid">
          <div className="s-price-input-col">
            <span className="s-price-sublabel">Min</span>
            <div className="s-price-input-wrap">
              <span className="s-price-symbol">{currency}</span>
              <input
                type="number"
                min="0"
                step="0.01"
                name="minPrice"
                aria-label={`Minimum price in ${currency}`}
                placeholder="0"
                defaultValue={params.get('minPrice') || ''}
              />
            </div>
          </div>
          <span className="s-price-divider" aria-hidden="true">—</span>
          <div className="s-price-input-col">
            <span className="s-price-sublabel">Max</span>
            <div className="s-price-input-wrap">
              <span className="s-price-symbol">{currency}</span>
              <input
                type="number"
                min="0"
                step="0.01"
                name="maxPrice"
                aria-label={`Maximum price in ${currency}`}
                placeholder="500+"
                defaultValue={params.get('maxPrice') || ''}
              />
            </div>
          </div>
        </div>
      </fieldset>

      {/* In stock only */}
      <div className="s-filter-group s-filter-checkbox-group">
        <label className="s-custom-checkbox">
          <input
            type="checkbox"
            name="inStock"
            value="true"
            defaultChecked={params.get('inStock') === 'true'}
          />
          <span className="s-checkbox-box" aria-hidden="true">
            <Icon name="check" />
          </span>
          <span className="s-checkbox-label">In stock only</span>
        </label>
      </div>

      {/* Actions */}
      <div className="s-filter-actions">
        <Button type="submit" className="s-filter-apply-btn">
          Apply filters
        </Button>
        {hasActiveFilters && (
          <button type="button" className="s-filter-clear-btn" onClick={clear}>
            <Icon name="x" /> Clear filters
          </button>
        )}
      </div>
    </form>
  );
}
