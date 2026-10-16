import { expect, it } from 'vitest';
import { catalogParams } from './catalogQuery';
  it('preserves URL filters while bounding server pagination', () => {
    expect(catalogParams(new URLSearchParams('search=flower&categoryId=3&collectionId=4&sort=price-asc&inStock=true&page=2'))).toEqual({ search: 'flower', categoryId: '3', collectionId: '4', sort: 'price-asc', inStock: true, pageNumber: 2, pageSize: 12 });
    expect(catalogParams(new URLSearchParams('page=-2&sort=unsupported'))).toEqual({ pageNumber: 1, pageSize: 12, sort: 'newest' });
  });
