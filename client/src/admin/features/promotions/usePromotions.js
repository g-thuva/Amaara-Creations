import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { useAdminMutation } from "../../shared/hooks/useAdminMutation.js";
import { promotionsApi } from "./promotionsApi.js";
export function usePromotions() {
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState(null);
  const [historyId, setHistoryId] = useState(null);
  const resource = useResource(
    (signal) => promotionsApi.listPromotions({ page }, signal),
    String(page),
  );
  const history = useResource(
    (signal) => promotionsApi.getRedemptions(historyId, signal),
    String(historyId),
    Boolean(historyId),
  );
  const mutation = useAdminMutation(resource.reload);
  const submit = async (event) => {
    event.preventDefault();
    const values = Object.fromEntries(new FormData(event.currentTarget));
    const nullable = (key) => (values[key] === "" ? null : Number(values[key]));
    const request = {
      ...values,
      discountValue: Number(values.discountValue),
      minimumSpend: nullable("minimumSpend"),
      maximumDiscount: nullable("maximumDiscount"),
      usageLimit: nullable("usageLimit"),
      perCustomerUsageLimit: nullable("perCustomerUsageLimit"),
      startsAt: new Date(values.startsAt).toISOString(),
      endsAt: values.endsAt ? new Date(values.endsAt).toISOString() : null,
      isActive: values.isActive === "on",
      appliesToStandard: values.appliesToStandard === "on",
      appliesToCustom: values.appliesToCustom === "on",
      rowVersion: editing.rowVersion,
    };
    const ok = await mutation.run(() =>
      editing.id
        ? promotionsApi.updatePromotion(editing.id, request)
        : promotionsApi.createPromotion(request),
    );
    if (ok) setEditing(null);
  };
  return {
    page,
    setPage,
    editing,
    setEditing,
    historyId,
    setHistoryId,
    resource,
    history,
    mutation,
    submit,
  };
}
