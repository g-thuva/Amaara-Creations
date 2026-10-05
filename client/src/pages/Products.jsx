import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useResource } from '../hooks/useResource';
import { productApi } from '../services/productApi';
import { contentApi } from '../services/contentApi';
import { catalogParams, sortOptions } from '../utils/storefront';
import ProductCard from '../components/storefront/ProductCard';
import { Button, Dialog, EmptyState, ErrorState, PageHeader, Pagination, Skeleton } from '../components/storefront/UI';
function Filters({ params, categories, collections, apply, clear }) {
  return <form className="s-filter-form" onSubmit={e => { e.preventDefault(); apply(new FormData(e.currentTarget)); }}>
    <label>Category<select name="categoryId" defaultValue={params.get('categoryId') || ''}><option value="">All categories</option>{categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label>
    <label>Collection<select name="collectionId" defaultValue={params.get('collectionId') || ''}><option value="">All collections</option>{collections.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label>
    <fieldset><legend>Price range (LKR)</legend><div className="s-price-range"><label>Minimum<input type="number" min="0" step="0.01" name="minPrice" defaultValue={params.get('minPrice') || ''}/></label><label>Maximum<input type="number" min="0" step="0.01" name="maxPrice" defaultValue={params.get('maxPrice') || ''}/></label></div></fieldset>
    <label className="s-checkbox"><input type="checkbox" name="inStock" value="true" defaultChecked={params.get('inStock') === 'true'}/> In stock only</label><Button type="submit">Apply filters</Button><Button type="button" variant="text" onClick={clear}>Clear filters</Button>
  </form>;
}
export default function Products() {
  const [params, setParams] = useSearchParams();
  const [open, setOpen] = useState(false);
  const [filterError, setFilterError] = useState('');
  const query = params.toString();
  const data = useResource(signal => productApi.getProducts(catalogParams(params), signal), query);
  const categories = useResource(contentApi.categories, 'categories');
  const collections = useResource(contentApi.collections, 'collections');
  const update = (values, reset = true) => { const next = new URLSearchParams(params); for (const [key, value] of Object.entries(values)) { if (value) next.set(key, value); else next.delete(key); } if (reset) next.delete('page'); setParams(next); };
  const clear = () => { setParams({}); setFilterError(''); setOpen(false); };
  const apply = form => {
    const values = Object.fromEntries(['categoryId', 'collectionId', 'minPrice', 'maxPrice', 'inStock'].map(k => [k, form.get(k) || '']));
    if (values.minPrice && values.maxPrice && Number(values.minPrice) > Number(values.maxPrice)) { setFilterError('Minimum price must be less than or equal to maximum price.'); return; }
    setFilterError(''); update({ ...values, category: '' }); setOpen(false);
  };
  const chips = [...params.entries()].filter(([k]) => ['search', 'category', 'categoryId', 'collectionId', 'minPrice', 'maxPrice', 'inStock'].includes(k));
  const chipLabel = (key, value) => key === 'categoryId' ? categories.data?.find(c => String(c.id) === value)?.name || 'Category' : key === 'collectionId' ? collections.data?.find(c => String(c.id) === value)?.name || 'Collection' : key === 'inStock' ? 'In stock' : `${({ search: 'Search', minPrice: 'From LKR', maxPrice: 'Up to LKR', category: 'Category' })[key]}: ${value}`;
  const filters = <><Filters key={query} params={params} categories={categories.data || []} collections={collections.data || []} apply={apply} clear={clear}/>{filterError && <p role="alert" className="s-error-text">{filterError}</p>}</>;
  return <div className="s-container s-page"><PageHeader eyebrow="The Amaara shop" title="Find a little something you.">Stickers, prints and personal touches. Explore what is available in the studio.</PageHeader>
    <div className="s-catalog-toolbar"><form key={params.get('search') || ''} className="s-search" onSubmit={e => { e.preventDefault(); update({ search: new FormData(e.currentTarget).get('search') }); }}><label className="sr-only" htmlFor="catalog-search">Search products</label><input id="catalog-search" name="search" type="search" defaultValue={params.get('search') || ''} placeholder="Search the shop"/><Button type="submit" variant="secondary">Search</Button></form><label className="s-sort">Sort by<select value={catalogParams(params).sort} onChange={e => update({ sort: e.target.value })}>{sortOptions.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><Button className="s-filter-trigger" variant="secondary" onClick={() => setOpen(true)}>Filters {chips.length ? `(${chips.length})` : ''}</Button></div>
    {chips.length > 0 && <div className="s-chips">{chips.map(([key, value]) => <button key={key} onClick={() => update({ [key]: '' })} aria-label={`Remove ${chipLabel(key, value)}`}>{chipLabel(key, value)} ×</button>)}<Button variant="text" onClick={clear}>Clear all</Button></div>}
    {(categories.error || collections.error) && <div className="s-alert">Some filters are unavailable. <Button variant="text" onClick={() => { categories.reload(); collections.reload(); }}>Retry filters</Button></div>}
    <div className="s-catalog-layout"><aside className="s-desktop-filters" aria-label="Product filters"><h2>Refine your search</h2>{filters}</aside><section aria-label="Products" aria-busy={data.loading}>{data.loading ? <Skeleton count={6}/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : <><p className="s-result-count" role="status">{data.data?.totalCount || 0} products</p>{data.data?.products.length ? <div className="s-product-grid s-catalog-grid">{data.data.products.map(product => <ProductCard key={product.id} product={product}/>)}</div> : <EmptyState title="No products found" to={null}>Try another search or clear your filters.<br/><Button variant="text" onClick={clear}>Clear all filters</Button></EmptyState>}<Pagination page={Number(catalogParams(params).pageNumber)} totalPages={data.data?.totalPages || 0} onChange={page => update({ page: String(page) }, false)}/></>}</section></div>
    <Dialog open={open} onClose={() => setOpen(false)} title="Product filters" drawer>{filters}</Dialog>
  </div>;
}
