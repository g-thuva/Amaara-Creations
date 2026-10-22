import { customerError } from '@/shared/api/apiError.ts';
import { EmptyState } from "@/features/admin/shared/components/EmptyState.jsx";
import { ErrorState } from './ErrorState.jsx';
import { Skeleton } from './Skeleton.jsx';

export function ResourceState({ resource, children, empty = false, errorTitle }) {
  const hasData = resource.data != null;
  if (resource.error && !hasData)
    return (
      <ErrorState title={errorTitle} message={resource.error.response?.status === 403 ? 'You do not have permission to view this information.' : customerError(resource.error)} retry={resource.reload} correlationId={resource.error.response?.data?.correlationId} />
    );
  if (resource.loading && !hasData)
    return <Skeleton />;
  return (
    <>
      {resource.loading && <p role="status">Refreshing details…</p>}
      {resource.error && (
        <ErrorState title={errorTitle || 'We couldn’t refresh this information'} message={customerError(resource.error)} retry={resource.reload} />
      )}
      {empty ? <EmptyState /> : children}
    </>
  );
}
