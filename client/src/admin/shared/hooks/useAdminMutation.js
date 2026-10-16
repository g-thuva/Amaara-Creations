import { useRef, useState } from "react";
import { customerError } from "../../../utils/storefront.ts";

// Presentation stays in MutationFeedback. The ref also blocks two submissions
// made before React commits the pending state.
export function useAdminMutation(reload) {
  const locked = useRef(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const reset = () => {
    setError(null);
    setSuccess(null);
  };
  const run = async (action, message = "Changes saved.") => {
    if (locked.current) return false;
    locked.current = true;
    setPending(true);
    reset();
    try {
      await action();
      setSuccess(message);
      reload?.();
      return true;
    } catch (failure) {
      setError(customerError(failure));
      return false;
    } finally {
      locked.current = false;
      setPending(false);
    }
  };
  return { pending, error, success, run, reset };
}
