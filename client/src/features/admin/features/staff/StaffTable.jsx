import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
export function StaffTable({ resource }) {
  return (
    <DataTable
      caption="Authorized staff"
      rows={resource.data?.items || []}
      columns={[
        { key: "name", label: "Name" },
        { key: "email", label: "Email" },
        { key: "roles", label: "Roles", render: (r) => r.roles.join(", ") },
      ]}
    />
  );
}
