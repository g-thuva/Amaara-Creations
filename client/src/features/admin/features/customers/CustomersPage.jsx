import { useCustomers } from "@/features/admin/features/customers/useCustomers.js";
import { CustomersTable } from "@/features/admin/features/customers/CustomersTable.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { PageHeader, FilterBar } from '@/features/admin/shared/components/AdminUi.jsx';
export default function CustomersPage() {
  const { page, setPage, search, setSearch, resource } = useCustomers();
  return (
    <div className="admin-page">
      <PageHeader title="Customers" description="Find an account and review its orders, delivery details and recorded activity." />
      <FilterBar><label>
        Search name or email
        <input
          className="form-control"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </label></FilterBar>
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
