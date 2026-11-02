import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { CustomDropdown } from '@/shared/components/ui/CustomDropdown.jsx';

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
        <CustomDropdown
          id="filter-category"
          name="categoryId"
          defaultValue={params.get('categoryId') || ''}
          ariaLabel="Filter by category"
          options={[
            { value: '', label: 'All categories' },
            ...categories.map((c) => ({ value: String(c.id), label: c.name })),
          ]}
        />
      </div>

      {/* Collection */}
      <div className="s-filter-group">
        <label className="s-filter-label" htmlFor="filter-collection">
          <span>Collection</span>
        </label>
        <CustomDropdown
          id="filter-collection"
          name="collectionId"
          defaultValue={params.get('collectionId') || ''}
          ariaLabel="Filter by collection"
          options={[
            { value: '', label: 'All collections' },
            ...collections.map((c) => ({ value: String(c.id), label: c.name })),
          ]}
        />
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
