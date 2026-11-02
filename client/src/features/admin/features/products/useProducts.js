import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { productsApi } from './productsApi.js';

export function useProducts() {
  const [query, setQuery] = useState({ page: 1, pageSize: 20, search: '', isActive: '' });
  const resource = useResource(signal => productsApi.getProducts({
    ...query, isActive: query.isActive === '' ? undefined : query.isActive === 'true',
  }, signal), JSON.stringify(query));
  const mutation = useAdminMutation(resource.reload);
  const toggleArchive = product => mutation.run(() => product.isActive
    ? productsApi.archiveProduct(product.id) : productsApi.reactivateProduct(product.id));
  return { query, setQuery, resource, mutation, toggleArchive };
}
