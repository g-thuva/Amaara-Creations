import { usePayments } from "./usePayments.js";
import { PaymentsTable } from "./PaymentsTable.jsx";
import { PaymentFilters } from "./PaymentFilters.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { Pagination } from "../../shared/components/Pagination.jsx";
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
