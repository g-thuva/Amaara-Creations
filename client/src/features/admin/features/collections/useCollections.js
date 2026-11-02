import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { collectionsApi } from './collectionsApi.js';
import { blank } from './collectionsForm.js';
import { productsApi } from '@/features/admin/features/products/productsApi.js';

export function useCollections() {
  const [form, setForm] = useState(blank);
  const [editing, setEditing] = useState(null);
  const [selectedProducts, setSelectedProducts] = useState(null);
  const [savedCollection, setSavedCollection] = useState(null);
  const resource = useResource(async signal => {
    const [items, productPage] = await Promise.all([collectionsApi.getCollections(signal), productsApi.getProducts({ pageSize: 100 }, signal)]);
    return { items, products: productPage.items || [] };
  }, 'collections');
  const selection = useResource(async signal => {
    const members = [];
    let page = 1;
    while (true) {
      const result = await productsApi.getProducts({ collectionId: editing.id, page, pageSize: 100 }, signal);
      members.push(...result.items);
      if (members.length >= (result.totalItems ?? result.totalCount) || result.items.length < 100) return members;
      page++;
    }
  }, String(editing?.id), Boolean(editing));
  const items = resource.data?.items || [];
  const products = [...new Map([...(resource.data?.products || []), ...(selection.data || [])].map(item => [item.id, item])).values()];
  const initialSelection = (selection.data || []).map(item => String(item.id));
  const selected = selectedProducts ?? initialSelection;
  const mutation = useAdminMutation();
  const selectionPending = Boolean(editing) && (selection.loading || Boolean(selection.error));
  const reset = () => { setForm(blank); setEditing(null); setSelectedProducts(null); setSavedCollection(null); mutation.reset(); };
  const save = async event => {
    event.preventDefault();
    if (selectionPending) return false;
    return mutation.run(async () => {
      const payload = { ...form, sortOrder: Number(form.sortOrder) };
      const current = editing || savedCollection;
      const saved = current ? await collectionsApi.updateCollection(current.id, payload) : await collectionsApi.createCollection(payload);
      if (!current) setSavedCollection(saved);
      await collectionsApi.replaceCollectionProducts(saved.id, selected.map(Number));
      setForm(blank); setEditing(null); setSelectedProducts(null); setSavedCollection(null); resource.reload();
    });
  };
  const edit = collection => { setEditing(collection); setForm(collection); setSelectedProducts(null); setSavedCollection(null); };
  const toggle = collection => mutation.run(async () => {
    if (collection.isActive) await collectionsApi.archiveCollection(collection.id);
    else await collectionsApi.reactivateCollection(collection.id);
    resource.reload();
  });
  const dirty = JSON.stringify(form) !== JSON.stringify(editing || blank)
    || (selectedProducts !== null && JSON.stringify([...selected].sort()) !== JSON.stringify([...initialSelection].sort()));
  return { items, products, form, setForm, editing, setEditing, selectedProducts: selected, setSelectedProducts,
    save, edit, toggle, resource, mutation, reset, dirty, selection, selectionPending };
}
