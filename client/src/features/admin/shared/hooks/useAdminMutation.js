import { useState } from 'react';
import { useMutation } from '@/shared/hooks/useMutation.js';
import { customerError } from '@/shared/api/apiError.ts';

export function useAdminMutation(reload) {
  const mutation = useMutation();
  const [success, setSuccess] = useState(null);
  const reset = () => { mutation.reset(); setSuccess(null); };
  const run = (action, message = 'Changes saved.') => {
    setSuccess(null);
    return mutation.run(action, { onSuccess: () => { setSuccess(message); reload?.(); } });
  };
  return { pending: mutation.pending, error: mutation.error ? customerError(mutation.error) : null, success, run, reset };
}
