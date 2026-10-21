import { safeJson } from '@/shared/utils/safeJson.ts';
const draftKey = 'amaara-custom-builder-draft';
export const blank = { name: 'My custom sticker', width: '', height: '', quantity: '', shapeCode: '', materialCode: '', finishCode: '', fontCode: '', colourCode: '', customText: '', textAlignment: 'center', rowVersion: null };
export const optionField = { Shape: 'shapeCode', Material: 'materialCode', Finish: 'finishCode', Font: 'fontCode', Colour: 'colourCode' };


export function readBuilderDraft(storage = sessionStorage) {
  try {
    const draft = safeJson(storage.getItem(draftKey));
    return draft && typeof draft === 'object' && !Array.isArray(draft) ? draft : null;
  } catch { return null; }
}
export function saveBuilderDraft(form, storage = sessionStorage) {
  try { storage.setItem(draftKey, JSON.stringify(form)); return true; } catch { return false; }
}
export function clearBuilderDraft(storage = sessionStorage) {
  try { storage.removeItem(draftKey); } catch { /* A saved design remains authoritative. */ }
}
