import { useCustomers } from "@/features/admin/features/customers/useCustomers.js";
import { CustomersTable } from "@/features/admin/features/customers/CustomersTable.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
export default function CustomersPage() {
  const { page, setPage, search, setSearch, resource } = useCustomers();
  return (
    <div className="admin-page">
      <h1>Customers</h1>
      <label>
        Search name or email
        <input
          className="form-control"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </label>
      <ResourceState resource={resource}>
        <CustomersTable resource={resource} />
        <Pagination
          page={page}
          data={resource.data}
          setPage={setPage}
          total={resource.data?.totalCount || 0}
          size={20}
        />
      </ResourceState>
    </div>
  );
}
