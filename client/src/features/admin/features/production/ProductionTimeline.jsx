import { formatDateTime } from "@/features/admin/shared/utils/format.js";
import { SnapshotViewer } from "@/features/admin/shared/components/SnapshotViewer.jsx";
import { Timeline, CopyableId } from '@/features/admin/shared/components/AdminUi.jsx';
export function ProductionTimeline({ data }) {
  return (
    <section className="card card-body">
      <h2>Job timeline</h2>
      {data.timeline?.length ? (
        <Timeline label="Production history">
          {data.timeline.map((entry, i) => (
            <li key={i}>
              {entry.action} · {formatDateTime(entry.createdAt)} · Actor{" "}
              <CopyableId value={entry.actorUserId} label="production actor identifier" />
              <SnapshotViewer value={entry.newValuesJson} />
            </li>
          ))}
        </Timeline>
      ) : (
        <p>No recorded transitions.</p>
      )}
    </section>
  );
}
