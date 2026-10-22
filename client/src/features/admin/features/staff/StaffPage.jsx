import { useStaff } from "@/features/admin/features/staff/useStaff.js";
import { StaffTable } from "@/features/admin/features/staff/StaffTable.jsx";
import { StaffRoleForm } from "@/features/admin/features/staff/StaffRoleForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
import { ConfirmDialog } from "@/features/admin/shared/components/ConfirmDialog.jsx";
import { PageHeader, Callout, StatusChip } from '@/features/admin/shared/components/AdminUi.jsx';
export default function StaffPage() {
  const staff = useStaff();
  const { resource, mutation, intent, setIntent } = staff;
  return (
    <div className="admin-page">
      <PageHeader title="Staff permissions" description="Assign access to verified accounts and review every staff change before saving." />
      <Callout tone="warning"><p>Replacing roles revokes refresh sessions. A second Super Admin must change your own access.</p></Callout>
      <MutationFeedback {...mutation} />
      <ResourceState resource={resource}>
        <StaffTable resource={resource} />
        <StaffRoleForm {...staff} />
        <form className="card card-body add-product-form" onSubmit={staff.submitRecovery}>
          <h2>Help a staff member reconnect their authenticator</h2>
          <p>Verify the person’s identity through your recovery procedure first. Approval ends their current sessions and gives them 15 minutes to start secure setup. A second owner must approve your own recovery.</p>
          <label>Recovery account email<input className="form-control" type="email" name="email" required /></label>
          <label>Verified recovery reason<textarea className="form-control" name="reason" minLength={10} maxLength={300} required /></label>
          <button className="btn btn-primary" disabled={mutation.pending}>Review authenticator recovery</button>
        </form>
      </ResourceState>
      <ConfirmDialog
        open={Boolean(intent)}
        onClose={() => setIntent(null)}
        title={intent?.kind === 'recovery' ? 'Approve authenticator recovery' : 'Confirm staff permissions'}
        pending={mutation.pending}
        onConfirm={staff.confirm}
      >
        {intent?.kind === 'recovery' ? <>Approve verified recovery for {intent.email}. Their existing sessions will end and their next sign-in will require a new verified authenticator. Reason: {intent.reason}</> : <span className="admin-review-summary"><strong>{intent?.email}</strong><span>Before: {(resource.data?.items?.find(item => item.email?.toLowerCase() === intent?.email?.toLowerCase())?.roles || []).join(', ') || 'No current staff roles in this directory'}</span><span>After: {intent?.roles?.length ? intent.roles.map(role => <StatusChip key={role} status={role} />) : 'No staff roles'}</span><span>Reason: {intent?.reason}</span></span>}
      </ConfirmDialog>
    </div>
  );
}
