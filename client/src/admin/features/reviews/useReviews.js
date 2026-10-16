import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { useAdminMutation } from "../../shared/hooks/useAdminMutation.js";
import { reviewsApi } from "./reviewsApi.js";
export function useReviews() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [intent, setIntent] = useState(null);
  const [reason, setReason] = useState("");
  const resource = useResource(
    (signal) =>
      reviewsApi.listReviews(
        { status: status || undefined, pageNumber: page },
        signal,
      ),
    JSON.stringify([page, status]),
  );
  const mutation = useAdminMutation(resource.reload);
  const confirm = async () => {
    await mutation.run(() =>
      reviewsApi.moderateReview(intent.id, { ...intent, reason }),
    );
    setIntent(null);
  };
  return {
    page,
    setPage,
    status,
    setStatus,
    intent,
    setIntent,
    reason,
    setReason,
    resource,
    mutation,
    confirm,
  };
}
