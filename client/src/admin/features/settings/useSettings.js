import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { useAdminMutation } from "../../shared/hooks/useAdminMutation.js";
import { settingsApi } from "./settingsApi.js";
import { blank } from "./settingsForm.js";
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
