import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { productsApi } from "@/features/admin/features/products/productsApi.js";
import { categoriesApi } from "@/features/admin/features/categories/categoriesApi.js";
import { collectionsApi } from "@/features/admin/features/collections/collectionsApi.js";
import { emptyForm } from "@/features/admin/features/products/productForm.js";
export function useProducts() {
  const [query, setQuery] = useState({
    page: 1,
    pageSize: 20,
    search: "",
    isActive: "",
  });
  const [formData, setFormData] = useState(emptyForm);
  const [editingProduct, setEditingProduct] = useState(null);
  // Pending media files to upload after product creation
  const [pendingMediaFiles, setPendingMediaFiles] = useState([]);

  const resource = useResource(
    (signal) =>
      productsApi.getProducts(
        {
          ...query,
          isActive:
            query.isActive === "" ? undefined : query.isActive === "true",
        },
        signal,
      ),
    JSON.stringify(query),
  );
  const lookups = useResource(async () => {
    const [categories, collections] = await Promise.all([
      categoriesApi.getCategories(),
      collectionsApi.getCollections(),
    ]);
    return { categories, collections };
  }, "product-lookups");
  const mutation = useAdminMutation();
  const loadProducts = resource.reload;
  const resetForm = () => {
    setEditingProduct(null);
    setFormData(emptyForm);
    setPendingMediaFiles([]);
  };

  const startEdit = (product) => {
    setEditingProduct(product);
    setPendingMediaFiles([]);
    setFormData({
      name: product.name || "",
      slug: product.slug || "",
      shortDescription: product.shortDescription || "",
      description: product.description || "",
      basePrice: product.basePrice?.toString() || "",
      currency: product.currency,
      rowVersion: product.rowVersion,
      baseSku: product.baseSku || "",
      categoryId: product.categoryId?.toString() || "",
      stock: product.stock?.toString() || "0",
      isActive: product.isActive,
      isFeatured: product.isFeatured,
      collectionIds: product.collectionIds || [],
    });
  };

  const saveProduct = async (event) => {
    event.preventDefault();
    const payload = {
      ...formData,
      basePrice: Number(formData.basePrice),
      stock: Number(formData.stock),
      categoryId: formData.categoryId ? Number(formData.categoryId) : null,
      collectionIds: formData.collectionIds.map(Number),
    };

    return mutation.run(async () => {
      let productId;
      if (editingProduct) {
        await productsApi.updateProduct(editingProduct.id, payload);
        productId = editingProduct.id;
      } else {
        const created = await productsApi.createProduct(payload);
        productId = created?.id ?? created?.productId;
      }
      // Upload any pending media files after save
      if (productId && pendingMediaFiles.length > 0) {
        for (let i = 0; i < pendingMediaFiles.length; i++) {
          const { file, altText } = pendingMediaFiles[i];
          await productsApi.uploadProductMedia(productId, file, {
            altText,
            isPrimary: i === 0,
            sortOrder: i,
          });
        }
      }
      resetForm();
      loadProducts();
    });
  };

  const toggleArchive = async (product) => {
    return mutation.run(async () => {
      if (product.isActive) {
        await productsApi.archiveProduct(product.id);
      } else {
        await productsApi.reactivateProduct(product.id);
      }
      loadProducts();
    });
  };

  return {
    query,
    setQuery,
    resource,
    lookups,
    mutation,
    formData,
    setFormData,
    editingProduct,
    resetForm,
    startEdit,
    saveProduct,
    toggleArchive,
    pendingMediaFiles,
    setPendingMediaFiles,
  };
}
