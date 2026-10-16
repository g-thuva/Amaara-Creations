import { formatDateTime } from "../../shared/utils/formatDateTime.js";
import { SnapshotViewer } from "../../shared/components/SnapshotViewer.jsx";
export function ProductionTimeline({ data }) {
  return (
    <section className="card card-body">
      <h2>Job timeline</h2>
      {data.timeline?.length ? (
        <ol>
          {data.timeline.map((entry, i) => (
            <li key={i}>
              {entry.action} · {formatDateTime(entry.createdAt)} · Actor{" "}
              {entry.actorUserId}
              <SnapshotViewer value={entry.newValuesJson} />
            </li>
          ))}
        </ol>
      ) : (
        <p>No recorded transitions.</p>
      )}
    </section>
  );
}
