import AppIcon from '@/shared/components/ui/Icon.jsx';
export function ErrorState({ title = 'We couldn’t load this information', message, retry, correlationId }) {
  return <div className="admin-error-state" role="alert"><span className="admin-state-icon"><AppIcon name="x" /></span><div><h2>{title}</h2>{message && <p>{message}</p>}{correlationId && <details><summary>Technical reference</summary><code>{correlationId}</code></details>}{retry && <button className="btn btn-outline-primary" onClick={retry}><AppIcon name="refresh" />Try again</button>}</div></div>;
}
