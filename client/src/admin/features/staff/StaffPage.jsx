import { useStaff } from "./useStaff.js";
import { StaffTable } from "./StaffTable.jsx";
import { StaffRoleForm } from "./StaffRoleForm.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog.jsx";
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
