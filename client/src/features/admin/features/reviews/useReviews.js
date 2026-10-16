import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { reviewsApi } from "@/features/admin/features/reviews/reviewsApi.js";
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
