/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useEffect } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { contentApi } from '@/features/content/contentApi.js';

const SiteContentContext = createContext(null);
export function SiteContentProvider({ enabled, children }) {
  const settings = useResource(contentApi.settings, 'settings', enabled);
  const reload = settings.reload;
  useEffect(() => {
    if (!enabled) return undefined;
    const refresh = () => { if (document.visibilityState === 'visible') reload(); };
    window.addEventListener('focus', refresh);
    document.addEventListener('visibilitychange', refresh);
    return () => { window.removeEventListener('focus', refresh); document.removeEventListener('visibilitychange', refresh); };
  }, [enabled, reload]);
  const values = Object.fromEntries((settings.data || []).map(item => [item.key, item.value]));
  return <SiteContentContext.Provider value={{ settings, values }}>{children}</SiteContentContext.Provider>;
}
export const useSiteContent = () => useContext(SiteContentContext);
