import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { catalogParams, sortOptions } from '@/features/catalog/catalogQuery.ts';

export default function CatalogToolbar({ catalog, categories = [] }) {
  const { params, update, setOpen, chips, chipLabel, clear } = catalog;
  const activeCategory = params.get('categoryId') || '';
  const visibleChips = chips.filter(([key]) => key !== 'categoryId');

  return (
    <div className="s-toolbar-wrapper">
      {categories.length > 0 && (
        <nav className="s-category-pills" aria-label="Shop by category">
          <button
            type="button"
            className={!activeCategory ? 'is-active' : ''}
            aria-pressed={!activeCategory}
            onClick={() => update({ categoryId: '', category: '' })}
          >
            All designs
          </button>
          {categories.map((category) => (
            <button
              key={category.id}
              type="button"
              className={activeCategory === String(category.id) ? 'is-active' : ''}
              aria-pressed={activeCategory === String(category.id)}
              onClick={() => update({ categoryId: String(category.id), category: '' })}
            >
              {category.name}
            </button>
          ))}
        </nav>
      )}

      <div className="s-catalog-toolbar">
        <form
          key={params.get('search') || ''}
          className="s-search-bar"
          role="search"
          onSubmit={(event) => {
            event.preventDefault();
            update({ search: new FormData(event.currentTarget).get('search') });
          }}
        >
          <div className="s-search-input-wrap">
            <Icon name="search" className="s-search-icon" aria-hidden="true" />
            <input
              id="catalog-search"
              name="search"
              type="search"
              defaultValue={params.get('search') || ''}
              placeholder="Search welcome signs, arches and stationery…"
              autoComplete="off"
            />
          </div>
          <Button type="submit" variant="primary" className="s-search-submit">
            Search
          </Button>
        </form>

        <div className="s-toolbar-controls">
          <div className="s-sort-wrapper">
            <span className="s-sort-label">Sort by:</span>
            <div className="s-sort-select-wrap">
              <select
                aria-label="Sort products"
                value={catalogParams(params).sort}
                onChange={(event) => update({ sort: event.target.value })}
              >
                {sortOptions.map(([value, label]) => (
                  <option key={value} value={value}>{label}</option>
                ))}
              </select>
              <Icon name="chevronDown" className="s-sort-chevron" aria-hidden="true" />
            </div>
          </div>

          <Button
            className="s-filter-trigger"
            variant="secondary"
            onClick={() => setOpen(true)}
          >
            <Icon name="sliders" />
            <span>Filters {chips.length ? `(${chips.length})` : ''}</span>
          </Button>
        </div>
      </div>

      {visibleChips.length > 0 && (
        <div className="s-chips" aria-label="Active filters">
          <span className="s-chips-heading">Active filters:</span>
          {visibleChips.map(([key, value]) => (
            <button
              key={key}
              type="button"
              className="s-chip-badge"
              onClick={() => update({ [key]: '' })}
              aria-label={`Remove ${chipLabel(key, value)}`}
            >
              <span>{chipLabel(key, value)}</span>
              <Icon name="x" />
            </button>
          ))}
          <button type="button" className="s-chip-clear-all" onClick={clear}>
            Clear all
          </button>
        </div>
      )}
    </div>
  );
}
