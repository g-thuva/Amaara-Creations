import { STORE_CURRENCY } from '@/shared/config/currency.js';
import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useResource } from '@/shared/hooks/useResource.js';
import { catalogApi } from '@/features/catalog/catalogApi.js';
import { catalogParams } from '@/features/catalog/catalogQuery.ts';

export function useCatalog() {
  const [params, setParams] = useSearchParams();
  const [open, setOpen] = useState(false);
  const [filterError, setFilterError] = useState('');
  const query = params.toString();
  const data = useResource((signal) => catalogApi.getProducts(catalogParams(params), signal), query);
  const categories = useResource(catalogApi.getCategories, 'categories');
  const collections = useResource(catalogApi.getCollections, 'collections');
  const currency = data.data?.currency || STORE_CURRENCY;

  const update = (values, reset = true) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(values)) {
      if (value) next.set(key, value);
      else next.delete(key);
    }
    if (reset) next.delete('page');
    setParams(next);
  };

  const clear = () => {
    setParams({});
    setFilterError('');
    setOpen(false);
  };

  const apply = (form) => {
    const values = Object.fromEntries(
      ['categoryId', 'collectionId', 'minPrice', 'maxPrice', 'inStock'].map((key) => [key, form.get(key) || '']),
    );
    if (values.minPrice && values.maxPrice && Number(values.minPrice) > Number(values.maxPrice)) {
      setFilterError('Minimum price must be less than or equal to maximum price.');
      return;
    }
    setFilterError('');
    update({ ...values, category: '' });
    setOpen(false);
  };

  const chips = [...params.entries()].filter(([key]) =>
    ['search', 'category', 'categoryId', 'collectionId', 'minPrice', 'maxPrice', 'inStock'].includes(key));
  const chipLabel = (key, value) => {
    if (key === 'categoryId') return categories.data?.find((item) => String(item.id) === value)?.name || 'Category';
    if (key === 'collectionId') return collections.data?.find((item) => String(item.id) === value)?.name || 'Collection';
    if (key === 'inStock') return 'In stock';
    const labels = { search: 'Search', minPrice: `From ${currency}`, maxPrice: `Up to ${currency}`, category: 'Category' };
    return `${labels[key]}: ${value}`;
  };

  return { params, open, setOpen, filterError, query, data, categories, collections, currency, update, clear, apply, chips, chipLabel };
}
