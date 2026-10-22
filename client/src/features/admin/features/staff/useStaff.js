import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { staffApi } from "@/features/admin/features/staff/staffApi.js";
export function useStaff() {
  const resource = useResource((signal) => staffApi.getStaff(signal));
  const [intent, setIntent] = useState(null);
  const mutation = useAdminMutation(resource.reload);
  const submit = (e) => {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setIntent({
      kind: 'roles',
      email: form.get("email"),
      reason: form.get("reason"),
      roles: form.getAll("roles"),
    });
  };
  const confirm = async () => {
    await mutation.run(() => intent.kind === 'recovery' ? staffApi.approveMfaRecovery({ email: intent.email, reason: intent.reason }) : staffApi.replaceRoles(intent));
    setIntent(null);
  };
  const submitRecovery = event => {
    event.preventDefault(); const values = Object.fromEntries(new FormData(event.currentTarget));
    setIntent({ kind: 'recovery', email: values.email, reason: values.reason });
  };
  return { resource, intent, setIntent, mutation, submit, submitRecovery, confirm };
}
