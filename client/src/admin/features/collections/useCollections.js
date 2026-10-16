import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { useAdminMutation } from "../../shared/hooks/useAdminMutation.js";
import { collectionsApi } from "./collectionsApi.js";
import { blank } from "./collectionsForm.js";
import { productsApi } from "../products/productsApi.js";
export function useCollections() {
  const [form, setForm] = useState(blank);
  const [editing, setEditing] = useState(null);
  const [selectedProducts, setSelectedProducts] = useState([]);

  const resource = useResource(async (signal) => {
    const [items, productPage] = await Promise.all([
      collectionsApi.getCollections(signal),
      productsApi.getProducts({ pageSize: 100 }, signal),
    ]);
    return { items, products: productPage.items || [] };
  }, "collections");
  const items = resource.data?.items || [];
  const products = resource.data?.products || [];
  const mutation = useAdminMutation();
  const load = resource.reload;

  const save = async (event) => {
    event.preventDefault();
    return mutation.run(async () => {
      const payload = { ...form, sortOrder: Number(form.sortOrder) };
      const saved = editing
        ? await collectionsApi.updateCollection(editing.id, payload)
        : await collectionsApi.createCollection(payload);
      await collectionsApi.replaceCollectionProducts(
        saved.id,
        selectedProducts.map(Number),
      );
      setForm(blank);
      setEditing(null);
      setSelectedProducts([]);
      await load();
    });
  };

  const edit = (collection) => {
    setEditing(collection);
    setForm(collection);
    setSelectedProducts([]);
  };

  const toggle = async (collection) => {
    return mutation.run(async () => {
      if (collection.isActive)
        await collectionsApi.archiveCollection(collection.id);
      else await collectionsApi.reactivateCollection(collection.id);
      await load();
    });
  };
  return {
    items,
    products,
    form,
    setForm,
    editing,
    setEditing,
    selectedProducts,
    setSelectedProducts,
    save,
    edit,
    toggle,
    resource,
    mutation,
  };
}
