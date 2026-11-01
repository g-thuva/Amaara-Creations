import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { catalogParams, sortOptions } from '@/features/catalog/catalogQuery.ts';

export default function CatalogToolbar({ catalog, categories = [] }) {
  const { params, update, setOpen, chips, chipLabel, clear } = catalog;
  const activeCategory = params.get('categoryId') || '';
  const visibleChips = chips.filter(([key]) => key !== 'categoryId');
  return <>
        {categories.length > 0 && (
          <nav className="s-category-pills" aria-label="Shop by category">
            <button type="button" className={!activeCategory ? 'is-active' : ''} aria-pressed={!activeCategory} onClick={() => update({ categoryId: '', category: '' })}>
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
          <form key={params.get('search') || ''} className="s-search" role="search" onSubmit={(event) => {
            event.preventDefault();
            update({ search: new FormData(event.currentTarget).get('search') });
          }}>
            <label className="sr-only" htmlFor="catalog-search">Search products</label>
            <Icon name="search" />
            <input id="catalog-search" name="search" type="search" defaultValue={params.get('search') || ''} placeholder="Search welcome signs, arches and stationery" />
            <Button type="submit" variant="secondary">Search</Button>
          </form>
          <label className="s-sort">
            Sort by
            <select value={catalogParams(params).sort} onChange={(event) => update({ sort: event.target.value })}>
              {sortOptions.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <Button className="s-filter-trigger" variant="secondary" onClick={() => setOpen(true)}>
            <Icon name="sliders" />Filters {chips.length ? `(${chips.length})` : ''}
          </Button>
        </div>

        {visibleChips.length > 0 && (
          <div className="s-chips" aria-label="Active filters">
            {visibleChips.map(([key, value]) => (
              <button key={key} type="button" onClick={() => update({ [key]: '' })} aria-label={`Remove ${chipLabel(key, value)}`}>
                {chipLabel(key, value)} <Icon name="x" />
              </button>
            ))}
            <Button variant="text" onClick={clear}>Clear all</Button>
          </div>
        )}

  </>;
}
