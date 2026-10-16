import { safeJson } from '@/shared/utils/safeJson.ts';
export function readCmsObject(content) {
  const value = safeJson(content);
  return value && typeof value === 'object' && !Array.isArray(value) ? value : null;
}
