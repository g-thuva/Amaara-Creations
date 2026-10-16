import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { useAdminMutation } from "../../shared/hooks/useAdminMutation.js";
import { staffApi } from "./staffApi.js";
export function useStaff() {
  const resource = useResource((signal) => staffApi.getStaff(signal));
  const [intent, setIntent] = useState(null);
  const mutation = useAdminMutation(resource.reload);
  const submit = (e) => {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setIntent({
      email: form.get("email"),
      reason: form.get("reason"),
      roles: form.getAll("roles"),
    });
  };
  const confirm = async () => {
    await mutation.run(() => staffApi.replaceRoles(intent));
    setIntent(null);
  };
  return { resource, intent, setIntent, mutation, submit, confirm };
}
