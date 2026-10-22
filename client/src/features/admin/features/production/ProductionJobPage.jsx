import { Link, useParams } from "react-router-dom";
import { ConfirmDialog } from "@/features/admin/shared/components/ConfirmDialog.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
import { useProductionJob } from "@/features/admin/features/production/useProductionJob.js";
import { ProductionJobSummary } from "@/features/admin/features/production/ProductionJobSummary.jsx";
import { PrivateAssetsList } from "@/features/admin/features/production/PrivateAssetsList.jsx";
import { ProofRevisionHistory } from "@/features/admin/features/production/ProofRevisionHistory.jsx";
import { ProductionTransitionForm } from "@/features/admin/features/production/ProductionTransitionForm.jsx";
import { ProductionTimeline } from "@/features/admin/features/production/ProductionTimeline.jsx";
import { ArchiveJobForm } from "@/features/admin/features/production/ArchiveJobForm.jsx";
import { PageHeader } from '@/features/admin/shared/components/PageHeader.jsx';
export default function ProductionJobPage() {
  const { id } = useParams();
  const job = useProductionJob(id);
  const { resource, mutation, data, design, archive, setArchive } = job;
  return (
    <div className="admin-page">
      <Link to="/admin/custom-designs">Custom production queue</Link>
      <PageHeader title="Production job" description="Review the specification, proof revisions and validated production progress." />
      <MutationFeedback {...mutation} />
      <ResourceState resource={resource}>
        {design && (
          <div className="admin-detail-grid"><div className="admin-detail-main">
            <PrivateAssetsList
              design={design}
              pending={mutation.pending}
              download={job.download}
            />
            <ProofRevisionHistory
              design={design}
              pending={mutation.pending}
              download={job.download}
              message={job.message}
              setMessage={job.setMessage}
              upload={job.upload}
            />
            <ProductionTransitionForm
              data={data}
              pending={mutation.pending}
              production={job.production}
            />
            {data.canArchive && (
              <ArchiveJobForm
                design={design}
                pending={mutation.pending}
                setArchive={setArchive}
              />
            )}
            </div><aside className="admin-detail-rail" aria-label="Production summary and timeline"><ProductionJobSummary data={data} design={design} /><ProductionTimeline data={data} /></aside>
            <ConfirmDialog
              open={Boolean(archive)}
              onClose={() => setArchive(null)}
              title="Archive fulfilled job"
              destructive
              pending={mutation.pending}
              onConfirm={job.confirmArchive}
            >
              {archive?.note}
            </ConfirmDialog>
          </div>
        )}
      </ResourceState>
    </div>
  );
}
