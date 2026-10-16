import { useRef, useState } from 'react';

// One in-flight action per consumer, including calls before React commits state.
export function useMutation() {
  const locked = useRef(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(null);
  const reset = () => setError(null);
  const run = async (action, { onSuccess, onError } = {}) => {
    if (locked.current) return false;
    locked.current = true;
    setPending(true);
    setError(null);
    try {
      const result = await action();
      onSuccess?.(result);
      return true;
    } catch (cause) {
      setError(cause);
      onError?.(cause);
      return false;
    } finally {
      locked.current = false;
      setPending(false);
    }
  };
  return { pending, error, run, reset };
}
