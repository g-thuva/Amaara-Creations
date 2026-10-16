import { useMutation } from '@/shared/hooks/useMutation.js';
import { useToast } from '@/shared/components/feedback/ToastProvider.jsx';
import { customerError } from '@/shared/api/apiError.ts';
import { useRequireLogin } from '@/features/auth/hooks/useRequireLogin.js';

// Authentication and customer feedback wrap the common mutation lifecycle.
export function useAuthenticatedMutation() {
  const mutation = useMutation();
  const requireLogin = useRequireLogin();
  const { notify } = useToast();
  const mutate = (action, refresh, message) => {
    if (!requireLogin()) return Promise.resolve(false);
    return mutation.run(action, {
      onSuccess: () => { refresh?.(); if (message) notify(message); },
      onError: cause => notify(customerError(cause), 'error'),
    });
  };
  return { pending: mutation.pending, mutate };
}
