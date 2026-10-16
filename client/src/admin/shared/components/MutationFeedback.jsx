export function MutationFeedback({ pending, error, success }) {
  return (
    <>
      {pending && <p role="status">Please wait…</p>}
      {error && <p role="alert">{error}</p>}
      {success && <p role="status">{success}</p>}
    </>
  );
}
