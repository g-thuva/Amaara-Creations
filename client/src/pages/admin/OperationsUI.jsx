import { useState } from 'react';
import { customerError } from '../../utils/storefront';

export function ResourceState({ resource, children }) {
  if (resource.error) return <div role="alert"><p>{resource.error.response?.status === 403 ? 'You do not have permission to view this information.' : customerError(resource.error)}</p><button className="btn btn-outline-primary" onClick={resource.reload}>Try again</button></div>;
  if (resource.loading) return <p role="status">Loading details…</p>;
  return children;
}

export function DataTable({ caption, columns, rows, rowKey = (row, i) => row.id ?? i }) {
  return <div className="table-responsive" tabIndex="0" role="region" aria-label={caption}><table className="admin-table"><caption>{caption}</caption><thead><tr>{columns.map(c => <th scope="col" key={c.key}>{c.label}</th>)}</tr></thead><tbody>{rows.length ? rows.map((r, i) => <tr key={rowKey(r, i)}>{columns.map(c => <td key={c.key}>{c.render ? c.render(r) : String(r[c.key] ?? '—')}</td>)}</tr>) : <tr><td colSpan={columns.length}>No records found.</td></tr>}</tbody></table></div>;
}

export function Pager({ page, setPage, total, size }) {
  return <nav className="action-buttons" aria-label="Results pages"><button className="btn btn-outline-primary" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button><span>Page {page} · {total} records</span><button className="btn btn-outline-primary" disabled={page * size >= total} onClick={() => setPage(page + 1)}>Next</button></nav>;
}

// The hook owns the accessible mutation feedback used by these admin forms.
// eslint-disable-next-line react-refresh/only-export-components
export function useMutation(reload) {
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState(null);
  const run = async (action, success = 'Changes saved.') => {
    if (pending) return false;
    setPending(true); setNotice(null);
    try { await action(); setNotice({ text: success }); reload?.(); return true; }
    catch (error) { setNotice({ error: true, text: customerError(error) }); return false; }
    finally { setPending(false); }
  };
  return { pending, run, feedback: notice && <p role={notice.error ? 'alert' : 'status'}>{notice.text}</p> };
}
