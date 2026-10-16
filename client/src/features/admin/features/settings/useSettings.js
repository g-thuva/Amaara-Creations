import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { settingsApi } from "@/features/admin/features/settings/settingsApi.js";
import { blank } from "@/features/admin/features/settings/settingsForm.js";
export function useSettings() {
  const [form, setForm] = useState(blank);

  const resource = useResource(settingsApi.getSettings, "settings");
  const items = resource.data || [];
  const mutation = useAdminMutation();
  const load = resource.reload;

  const save = async (event) => {
    event.preventDefault();
    return mutation.run(async () => {
      await settingsApi.saveSetting(form);
      setForm(blank);
      await load();
    });
  };
  return { items, form, setForm, save, resource, mutation };
}
