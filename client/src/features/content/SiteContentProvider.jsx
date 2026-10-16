/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { contentApi } from '@/features/content/contentApi.js';

const SiteContentContext = createContext(null);
export function SiteContentProvider({ enabled, children }) {
  const settings = useResource(contentApi.settings, 'settings', enabled);
  const values = Object.fromEntries((settings.data || []).map(item => [item.key, item.value]));
  return <SiteContentContext.Provider value={{ settings, values }}>{children}</SiteContentContext.Provider>;
}
export const useSiteContent = () => useContext(SiteContentContext);
