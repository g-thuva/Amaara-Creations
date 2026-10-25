import { STORE_CURRENCY } from '@/shared/config/currency.js';
export const emptyForm = {
  name: "",
  slug: "",
  shortDescription: "",
  description: "",
  basePrice: "",
  currency: STORE_CURRENCY,
  rowVersion: "",
  baseSku: "",
  categoryId: "",
  stock: "0",
  isActive: true,
  isFeatured: false,
  collectionIds: [],
};
