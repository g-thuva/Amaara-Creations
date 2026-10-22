import { useEffect } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { useToast } from './Toast.jsx';
export function MutationFeedback({ pending, error, success }) {
  const notify = useToast();
  useEffect(() => { if (success) notify?.(success); if (error) notify?.(error, 'error'); }, [success, error, notify]);
  return (
    <>
      {pending && <p role="status"><span className="admin-spinner" aria-hidden="true" /> Please wait…</p>}
      {error && <p role="alert" className="admin-callout admin-callout-danger"><AppIcon name="x" />{error}</p>}
      {success && <p role="status" className="admin-feedback-success"><AppIcon name="check" />{success}</p>}
    </>
  );
}
