import { useCallback, useEffect, useRef, useState } from 'react';

// The repository uses Axios and local state. Abort plus an effect-local guard
// prevents stale responses from replacing data after navigation/filter changes.
export function useResource(loader, key = '', enabled = true, { keepPreviousData = false } = {}) {
  const loaderRef = useRef(loader);
  loaderRef.current = loader;
  const [revision, setRevision] = useState(0);
  const generation = useRef(0);
  const request = useRef(null);
  const [state, setState] = useState({ key: null, revision: -1, data: null, loading: enabled, error: null });
  const reload = useCallback(() => {
    // Invalidate synchronously, before React runs the next effect's cleanup.
    generation.current++;
    request.current?.abort();
    setRevision(v => v + 1);
  }, []);
  useEffect(() => {
    if (!enabled) return;
    const controller = new AbortController();
    request.current = controller;
    const currentGeneration = generation.current;
    let active = true;
    const retainedData = previous => keepPreviousData && previous.key === key ? previous.data : null;
    setState(previous => ({ key, revision, data: retainedData(previous), loading: true, error: null }));
    Promise.resolve().then(() => loaderRef.current(controller.signal)).then(data => {
      if (active && !controller.signal.aborted && currentGeneration === generation.current) setState({ key, revision, data, loading: false, error: null });
    }).catch(error => {
      if (active && !controller.signal.aborted && currentGeneration === generation.current && error.code !== 'ERR_CANCELED') setState(previous => ({ key, revision, data: retainedData(previous), loading: false, error }));
    });
    return () => { active = false; controller.abort(); };
  }, [key, revision, enabled, keepPreviousData]);
  if (!enabled) return { data: null, loading: false, error: null, reload };
  return { ...(state.key === key ? { ...state, loading: state.loading || state.revision !== revision } : { data: null, loading: true, error: null }), reload };
}
