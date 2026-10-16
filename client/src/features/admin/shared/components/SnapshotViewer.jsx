export function SnapshotViewer({ value, empty = "No snapshot is available." }) {
  if (!value) return <p>{empty}</p>;
  try {
    const snapshot = typeof value === "string" ? JSON.parse(value) : value;
    return <pre>{JSON.stringify(snapshot, null, 2)}</pre>;
  } catch {
    return (
      <div role="status">
        <p>This historical snapshot could not be read.</p>
        <details>
          <summary>View original snapshot</summary>
          <pre>{String(value)}</pre>
        </details>
      </div>
    );
  }
}
