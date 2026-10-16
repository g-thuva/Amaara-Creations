import { formatDate } from "../../shared/utils/formatDateTime.js";
import { DataTable } from "../../shared/components/DataTable.jsx";
import { Link } from "react-router-dom";
export function CustomersTable({ resource }) {
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
        {
          key: "createdAt",
          label: "Joined",
          render: (r) => formatDate(r.createdAt),
        },
      ]}
    />
  );
}
