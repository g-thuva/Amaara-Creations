import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { contentApi } from "@/features/admin/features/content/contentApi.js";
import { blankPage } from "@/features/admin/features/content/contentForm.js";
export function useContent() {
  const [form, setForm] = useState(blankPage);
  const [editing, setEditing] = useState(null);

  const resource = useResource(contentApi.getPages, "content");
  const pages = resource.data || [];
  const mutation = useAdminMutation();
  const load = resource.reload;

  const save = async (event) => {
    event.preventDefault();
    return mutation.run(async () => {
      if (editing) await contentApi.updatePage(editing.id, form);
      else await contentApi.createPage(form);
      setForm(blankPage);
      setEditing(null);
      await load();
    });
  };

  const edit = (page) => {
    setEditing(page);
    setForm({
      ...page,
      sections: page.sections.length ? page.sections : blankPage.sections,
    });
  };

  const updateSection = (index, field, value) => {
    setForm({
      ...form,
      sections: form.sections.map((section, current) =>
        current === index ? { ...section, [field]: value } : section,
      ),
    });
  };
  return {
    pages,
    form,
    setForm,
    editing,
    setEditing,
    save,
    edit,
    updateSection,
    resource,
    mutation,
  };
}
