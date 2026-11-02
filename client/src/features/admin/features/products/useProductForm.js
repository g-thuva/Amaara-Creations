import { useRef, useState } from 'react';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { productsApi } from './productsApi.js';
import { emptyForm } from './productForm.js';

function initialForm(product) {
  if (!product) return { ...emptyForm, collectionIds: [] };
  return {
    name: product.name || '', slug: product.slug || '', shortDescription: product.shortDescription || '',
    description: product.description || '', basePrice: String(product.basePrice ?? ''), currency: product.currency,
    rowVersion: product.rowVersion, baseSku: product.baseSku || '', categoryId: String(product.categoryId ?? ''),
    stock: String(product.stock ?? 0), isActive: product.isActive, isFeatured: product.isFeatured,
    collectionIds: (product.collectionIds || []).map(String),
  };
}

export function useProductForm(product, onSaved, existingMediaCount = 0) {
  const [baseline] = useState(() => initialForm(product));
  const [formData, setFormData] = useState(baseline);
  const [pendingMediaFiles, setPendingMediaFiles] = useState([]);
  const [savedProduct, setSavedProduct] = useState(null);
  const committed = useRef(null);
  const uploaded = useRef(0);
  const mutation = useAdminMutation();
  const saveProduct = async event => {
    event.preventDefault();
    return mutation.run(async () => {
      // Media can fail after a successful create. Retrying must never create a second product.
      if (!committed.current) {
        const payload = {
          ...formData, basePrice: Number(formData.basePrice), stock: Number(formData.stock),
          categoryId: formData.categoryId ? Number(formData.categoryId) : null,
          collectionIds: formData.collectionIds.map(Number),
        };
        const response = product ? await productsApi.updateProduct(product.id, payload) : await productsApi.createProduct(payload);
        committed.current = { ...payload, ...response, id: product?.id ?? response.id ?? response.productId };
        setSavedProduct(committed.current);
      }
      for (const item of pendingMediaFiles) {
        await productsApi.uploadProductMedia(committed.current.id, item.file, {
          altText: item.altText, isPrimary: existingMediaCount === 0 && uploaded.current === 0,
          sortOrder: existingMediaCount + uploaded.current,
        });
        uploaded.current++;
        setPendingMediaFiles(current => current.filter(file => file !== item));
      }
      onSaved(committed.current);
    }, product ? 'Product updated.' : 'Product created.');
  };
  return {
    formData, setFormData, pendingMediaFiles, setPendingMediaFiles, saveProduct, mutation, savedProduct,
    dirty: JSON.stringify(formData) !== JSON.stringify(baseline) || pendingMediaFiles.length > 0,
  };
}
