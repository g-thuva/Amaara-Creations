import { safeJson } from '@/shared/utils/safeJson.ts';
const draftKey = 'amaara-custom-builder-draft';
const keyFor = designKey => designKey ? `${draftKey}:${designKey}` : draftKey;
export const blank = { name: 'My custom sticker', width: '', height: '', quantity: '', shapeCode: '', materialCode: '', finishCode: '', fontCode: '', colourCode: '', customText: '', textAlignment: 'center', rowVersion: null };
export const optionField = { Shape: 'shapeCode', Material: 'materialCode', Finish: 'finishCode', Font: 'fontCode', Colour: 'colourCode' };


export function readBuilderDraft(storage = sessionStorage, designKey = null) {
  try {
    const draft = safeJson(storage.getItem(keyFor(designKey)));
    return draft && typeof draft === 'object' && !Array.isArray(draft) ? draft : null;
  } catch { return null; }
}
export function saveBuilderDraft(form, storage = sessionStorage, designKey = null) {
  try { storage.setItem(keyFor(designKey), JSON.stringify(form)); return true; } catch { return false; }
}
export function clearBuilderDraft(storage = sessionStorage, designKey = null) {
  try { storage.removeItem(keyFor(designKey)); } catch { /* A saved design remains authoritative. */ }
}
