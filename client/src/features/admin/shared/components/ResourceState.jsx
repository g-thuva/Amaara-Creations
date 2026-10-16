import { customerError } from '@/shared/api/apiError.ts';
import { EmptyState } from "@/features/admin/shared/components/EmptyState.jsx";

export function ResourceState({ resource, children, empty = false }) {
  const hasData = resource.data != null;
  if (resource.error && !hasData)
    return (
      <div role="alert">
        <p>
          {resource.error.response?.status === 403
            ? "You do not have permission to view this information."
            : customerError(resource.error)}
        </p>
        <button className="btn btn-outline-primary" onClick={resource.reload}>
          Try again
        </button>
      </div>
    );
  if (resource.loading && !hasData)
    return <p role="status">Loading details…</p>;
  return (
    <>
      {resource.loading && <p role="status">Refreshing details…</p>}
      {resource.error && (
        <div role="alert">
          <p>{customerError(resource.error)}</p>
          <button onClick={resource.reload}>Try again</button>
        </div>
      )}
      {empty ? <EmptyState /> : children}
    </>
  );
}
