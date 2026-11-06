import { formatLocalDate as formatDate } from "@/features/admin/shared/utils/format.js";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { Link } from "react-router-dom";
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
import { RowActions } from '@/features/admin/shared/components/RowActions.jsx';
import { Button } from '@/features/admin/shared/components/AdminUi.jsx';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { can } from '@/app/config/permissions.js';
import { availableCustomerActions, customerActions } from './customerAccountActions.js';
export function CustomersTable({ resource, onAction }) {
  const { user } = useAuth();
  return (
    <DataTable
      caption="Registered customers"
      rows={resource.data?.customers || []}
      columns={[
        {
          key: "name",
          label: "Customer",
          render: (r) => <Link to={`/admin/customers/${r.id}`}>{r.name}</Link>,
        },
        { key: "email", label: "Email" },
        { key: "phone", label: "Phone" },
        { key: 'accountStatus', label: 'Account status', render: row => <StatusChip status={row.accountStatus} /> },
        { key: 'orderCount', label: 'Orders' },
        {
          key: "createdAt",
          label: "Joined",
          render: (r) => formatDate(r.createdAt),
        },
        ...(can(user, 'ManageCustomerAccess') ? [{
          key: 'actions', label: 'Account actions',
          render: customer => {
            const actions = availableCustomerActions(customer, user);
            return actions.length ? <RowActions label={`Manage customer ${customer.email}`}>
              {actions.map(action => <Button key={action} variant={customerActions[action].variant}
                disabled={resource.loading || Boolean(resource.error)} onClick={event => { event.currentTarget.closest('details')?.querySelector('summary')?.focus(); onAction(customer, action); }}>{customerActions[action].label}</Button>)}
            </RowActions> : <span className="admin-helper">No account actions</span>;
          },
        }] : []),
      ]}
    />
  );
}
