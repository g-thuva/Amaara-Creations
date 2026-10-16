import { useStaff } from "@/features/admin/features/staff/useStaff.js";
import { StaffTable } from "@/features/admin/features/staff/StaffTable.jsx";
import { StaffRoleForm } from "@/features/admin/features/staff/StaffRoleForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
import { ConfirmDialog } from "@/shared/components/ui/ConfirmDialog.jsx";
export default function StaffPage() {
  const staff = useStaff();
  const { resource, mutation, intent, setIntent } = staff;
  return (
    <div className="admin-page">
      <h1>Staff permissions</h1>
      <p>
        Assign access to an existing verified account. Replacing roles revokes
        refresh sessions. A second Super Admin must change your own access.
      </p>
      <MutationFeedback {...mutation} />
      <ResourceState resource={resource}>
        <StaffTable resource={resource} />
        <StaffRoleForm {...staff} />
      </ResourceState>
      <ConfirmDialog
        open={Boolean(intent)}
        onClose={() => setIntent(null)}
        title="Confirm staff permissions"
        pending={mutation.pending}
        onConfirm={staff.confirm}
      >
        Assign {intent?.roles.join(", ") || "no staff roles"} to {intent?.email}
        . Reason: {intent?.reason}
      </ConfirmDialog>
    </div>
  );
}
