import { useState } from 'react';
import { useCustomers } from "@/features/admin/features/customers/useCustomers.js";
import { CustomersTable } from "@/features/admin/features/customers/CustomersTable.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { PageHeader, FilterBar } from '@/features/admin/shared/components/AdminUi.jsx';
import { CustomerAccountDialog } from './CustomerAccountDialog.jsx';
import { MutationFeedback } from '@/features/admin/shared/components/MutationFeedback.jsx';
export default function CustomersPage() {
  const { page, setPage, search, setSearch, status, sort, setFilters, resource } = useCustomers();
  const [selection, setSelection] = useState(null);
  const [success, setSuccess] = useState(null);
  const saved = message => {
    const remainsVisible = status === 'all' || (status ? selection.action === status : selection.action !== 'Removed');
    setSelection(null);
    setSuccess(message);
    if (page > 1 && resource.data?.customers?.length === 1 && !remainsVisible) setPage(page - 1);
    else resource.reload();
  };
  return (
    <div className="admin-page">
      <PageHeader title="Customers" description="Manage customer access and review orders, delivery details and account history." />
      <MutationFeedback success={success} />
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
      </label><label>Account status<select aria-label="Account status" className="form-control" value={status} onChange={e => setFilters({ status: e.target.value })}><option value="">Current customers</option><option value="all">All statuses, including removed</option>{['Active', 'Suspended', 'Blocked', 'PendingDeletion', 'Removed'].map(value => <option key={value}>{value}</option>)}</select></label><label>Sort customers<select aria-label="Sort customers" className="form-control" value={sort} onChange={e => setFilters({ sort: e.target.value })}><option value="newest">Newest first</option><option value="oldest">Oldest first</option><option value="name">Name A–Z</option></select></label></FilterBar>
      <p className="admin-helper">Removed accounts are hidden from the current list. Choose Removed to find an account and restore access.</p>
      <ResourceState resource={resource}>
        <CustomersTable resource={resource} onAction={(customer, action) => { setSuccess(null); setSelection({ customer, action }); }} />
        <Pagination
          page={page}
          data={resource.data}
          setPage={setPage}
          total={resource.data?.totalCount || 0}
          size={20}
        />
      </ResourceState>
      {selection && <CustomerAccountDialog key={`${selection.customer.id}-${selection.action}`} {...selection}
        stale={resource.loading || Boolean(resource.error)} onClose={() => setSelection(null)}
        onSaved={saved} />}
    </div>
  );
}
