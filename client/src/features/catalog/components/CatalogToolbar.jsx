import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { catalogParams, sortOptions } from '@/features/catalog/catalogQuery.ts';

export default function CatalogToolbar({ catalog }) {
  const { params, update, setOpen, chips, chipLabel, clear } = catalog;
  return <>
        <div className="s-catalog-toolbar">
          <form key={params.get('search') || ''} className="s-search" role="search" onSubmit={(event) => {
            event.preventDefault();
            update({ search: new FormData(event.currentTarget).get('search') });
          }}>
            <label className="sr-only" htmlFor="catalog-search">Search products</label>
            <Icon name="search" />
            <input id="catalog-search" name="search" type="search" defaultValue={params.get('search') || ''} placeholder="Search stickers, decals and prints" />
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

        {chips.length > 0 && (
          <div className="s-chips" aria-label="Active filters">
            {chips.map(([key, value]) => (
              <button key={key} type="button" onClick={() => update({ [key]: '' })} aria-label={`Remove ${chipLabel(key, value)}`}>
                {chipLabel(key, value)} <Icon name="x" />
              </button>
            ))}
            <Button variant="text" onClick={clear}>Clear all</Button>
          </div>
        )}

  </>;
}
