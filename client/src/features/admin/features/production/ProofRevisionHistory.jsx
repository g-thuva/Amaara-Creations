import { formatDateTime } from "@/features/admin/shared/utils/format.js";
import { ProofUploadForm } from "@/features/admin/features/production/ProofUploadForm.jsx";
import { StatusChip, EmptyState } from '@/features/admin/shared/components/AdminUi.jsx';
export function ProofRevisionHistory({
  design,
  pending,
  download,
  message,
  setMessage,
  upload,
}) {
  return (
    <section className="card card-body">
      <h2>Proof revision history</h2>
      <ol>
        {design.proofRevisions.map((proof) => (
          <li key={proof.id}>
            Revision {proof.revisionNumber} · <StatusChip status={proof.status} /> ·{" "}
            {formatDateTime(proof.createdAt)}
            <p>{proof.customerVisibleMessage}</p>
            {proof.customerResponse && (
              <p>
                Customer: {proof.customerResponse} ·{" "}
                {formatDateTime(proof.customerResponseAt)}
              </p>
            )}
            <button
              className="btn btn-outline-primary"
              disabled={pending}
              onClick={() =>
                download({
                  downloadUrl: proof.downloadUrl,
                  originalFileName: `proof-${proof.revisionNumber}`,
                })
              }
            >
              Download proof {proof.revisionNumber}
            </button>
          </li>
        ))}
      </ol>
      {!design.proofRevisions.length && <EmptyState title="No proof revisions." description="Upload a proof below when it is ready for the customer to review." icon="image" />}
      <ProofUploadForm
        message={message}
        setMessage={setMessage}
        pending={pending}
        upload={upload}
      />
    </section>
  );
}
