import { usePayments } from "@/features/admin/features/payments/usePayments.js";
import { PaymentsTable } from "@/features/admin/features/payments/PaymentsTable.jsx";
import { PaymentFilters } from "@/features/admin/features/payments/PaymentFilters.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
export default function PaymentsPage() {
  const { page, setPage, status, setStatus, mode, setMode, resource } =
    usePayments();
  return (
    <div className="admin-page">
      <h1>Payments and refunds</h1>
      <PaymentFilters
        status={status}
        setStatus={setStatus}
        mode={mode}
        setMode={setMode}
      />
      <ResourceState resource={resource}>
        <PaymentsTable resource={resource} />
        <Pagination
          page={page}
          data={resource.data}
          setPage={setPage}
          size={30}
          total={resource.data?.totalCount || 0}
        />
      </ResourceState>
    </div>
  );
}
