import { Button } from '@/shared/components/ui/Button.jsx';
export default function ProductFilters({ params, categories, collections, apply, clear, currency }) {
  return (
    <form className="s-filter-form" onSubmit={(event) => {
      event.preventDefault();
      apply(new FormData(event.currentTarget));
    }}>
      <label>
        Category
        <select name="categoryId" defaultValue={params.get('categoryId') || ''}>
          <option value="">All categories</option>
          {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
        </select>
      </label>
      <label>
        Collection
        <select name="collectionId" defaultValue={params.get('collectionId') || ''}>
          <option value="">All collections</option>
          {collections.map((collection) => <option key={collection.id} value={collection.id}>{collection.name}</option>)}
        </select>
      </label>
      <fieldset>
        <legend>Price range ({currency})</legend>
        <div className="s-price-range">
          <label>Minimum<input type="number" min="0" step="0.01" name="minPrice" defaultValue={params.get('minPrice') || ''} /></label>
          <label>Maximum<input type="number" min="0" step="0.01" name="maxPrice" defaultValue={params.get('maxPrice') || ''} /></label>
        </div>
      </fieldset>
      <label className="s-checkbox">
        <input type="checkbox" name="inStock" value="true" defaultChecked={params.get('inStock') === 'true'} />
        In stock only
      </label>
      <Button type="submit">Apply filters</Button>
      <Button type="button" variant="text" onClick={clear}>Clear filters</Button>
    </form>
  );
}
