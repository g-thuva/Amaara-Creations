import { Link, useParams } from "react-router-dom";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
import { useProductionJob } from "./useProductionJob.js";
import { ProductionJobSummary } from "./ProductionJobSummary.jsx";
import { PrivateAssetsList } from "./PrivateAssetsList.jsx";
import { ProofRevisionHistory } from "./ProofRevisionHistory.jsx";
import { ProductionTransitionForm } from "./ProductionTransitionForm.jsx";
import { ProductionTimeline } from "./ProductionTimeline.jsx";
import { ArchiveJobForm } from "./ArchiveJobForm.jsx";
export default function ProductionJobPage() {
  const { id } = useParams();
  const job = useProductionJob(id);
  const { resource, mutation, data, design, archive, setArchive } = job;
  return (
    <div className="admin-page">
      <Link to="/admin/custom-designs">Custom production queue</Link>
      <h1>Production job</h1>
      <MutationFeedback {...mutation} />
      <ResourceState resource={resource}>
        {design && (
          <>
            <ProductionJobSummary data={data} design={design} />
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
            <ProductionTimeline data={data} />
            {data.canArchive && (
              <ArchiveJobForm
                design={design}
                pending={mutation.pending}
                setArchive={setArchive}
              />
            )}
            <ConfirmDialog
              open={Boolean(archive)}
              onClose={() => setArchive(null)}
              title="Archive fulfilled job"
              pending={mutation.pending}
              onConfirm={job.confirmArchive}
            >
              {archive?.note}
            </ConfirmDialog>
          </>
        )}
      </ResourceState>
    </div>
  );
}
