import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { useAdminMutation } from "../../shared/hooks/useAdminMutation.js";
import { categoriesApi } from "./categoriesApi.js";
import { blank } from "./categoriesForm.js";
export function useCategories() {
  const [form, setForm] = useState(blank);
  const [editing, setEditing] = useState(null);

  const resource = useResource(categoriesApi.getCategories, "categories");
  const items = resource.data || [];
  const mutation = useAdminMutation();
  const load = resource.reload;

  const save = async (event) => {
    event.preventDefault();
    return mutation.run(async () => {
      const payload = {
        ...form,
        parentCategoryId: form.parentCategoryId
          ? Number(form.parentCategoryId)
          : null,
        sortOrder: Number(form.sortOrder),
      };
      if (editing) await categoriesApi.updateCategory(editing.id, payload);
      else await categoriesApi.createCategory(payload);
      setForm(blank);
      setEditing(null);
      await load();
    });
  };

  const edit = (category) => {
    setEditing(category);
    setForm({ ...category, parentCategoryId: category.parentCategoryId || "" });
  };

  const toggle = async (category) => {
    return mutation.run(async () => {
      if (category.isActive) await categoriesApi.archiveCategory(category.id);
      else await categoriesApi.reactivateCategory(category.id);
      await load();
    });
  };
  return {
    items,
    form,
    setForm,
    editing,
    setEditing,
    save,
    edit,
    toggle,
    resource,
    mutation,
  };
}
