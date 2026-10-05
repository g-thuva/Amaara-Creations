import { useCallback, useEffect, useRef, useState } from 'react';

// The repository uses Axios and local state. Abort plus an effect-local guard
// prevents stale responses from replacing data after navigation/filter changes.
export function useResource(loader, key = '', enabled = true) {
  const loaderRef = useRef(loader);
  loaderRef.current = loader;
  const [revision, setRevision] = useState(0);
  const [state, setState] = useState({ key: null, data: null, loading: enabled, error: null });
  const reload = useCallback(() => setRevision(v => v + 1), []);
  useEffect(() => {
    if (!enabled) return;
    const controller = new AbortController();
    let active = true;
    setState({ key, data: null, loading: true, error: null });
    Promise.resolve().then(() => loaderRef.current(controller.signal)).then(data => {
      if (active) setState({ key, data, loading: false, error: null });
    }).catch(error => {
      if (active && error.code !== 'ERR_CANCELED') setState({ key, data: null, loading: false, error });
    });
    return () => { active = false; controller.abort(); };
  }, [key, revision, enabled]);
  if (!enabled) return { data: null, loading: false, error: null, reload };
  return { ...(state.key === key ? state : { data: null, loading: true, error: null }), reload };
}
