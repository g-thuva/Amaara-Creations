import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { ReviewModerationForm } from "@/features/admin/features/reviews/ReviewModerationForm.jsx";
export function ReviewsTable({ resource, pending, review }) {
  return (
    <DataTable
      caption="Customer reviews"
      rows={resource.data?.reviews || []}
      columns={[
        { key: "productName", label: "Product" },
        { key: "userName", label: "Customer" },
        { key: "rating", label: "Rating" },
        { key: "comment", label: "Review" },
        {
          key: "verifiedPurchase",
          label: "Purchase",
          render: (r) =>
            r.verifiedPurchase ? "Verified purchase" : "Unverified",
        },
        { key: "moderationStatus", label: "State" },
        { key: "moderationReason", label: "Last reason" },
        {
          key: "actions",
          label: "Moderate",
          render: (r) => (
            <ReviewModerationForm row={r} pending={pending} review={review} />
          ),
        },
      ]}
    />
  );
}
