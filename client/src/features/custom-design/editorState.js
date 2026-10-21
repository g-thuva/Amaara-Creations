import { safeJson } from '@/shared/utils/safeJson.ts';

export const defaultEditor = Object.freeze({ schemaVersion: 2, artworkFit: 'contain', artworkScale: 1, artworkX: 50, artworkY: 50, artworkRotation: 0, textX: 50, textY: 50, textSize: 12, background: 'white' });
const ranges = { artworkScale: [.25, 4], artworkX: [0, 100], artworkY: [0, 100], artworkRotation: [-180, 180], textX: [0, 100], textY: [0, 100], textSize: [2, 40] };
export function readEditorState(value) {
  const parsed = typeof value === 'string' ? safeJson(value) : value;
  const state = { ...defaultEditor };
  if (parsed?.schemaVersion !== 2) return state;
  for (const [field, [min, max]] of Object.entries(ranges)) {
    const number = Number(parsed[field]);
    if (Number.isFinite(number)) state[field] = Math.min(max, Math.max(min, number));
  }
  if (['contain', 'cover'].includes(parsed.artworkFit)) state.artworkFit = parsed.artworkFit;
  if (['white', 'transparent'].includes(parsed.background)) state.background = parsed.background;
  return state;
}

export function artworkPlacement(imageWidth, imageHeight, width, height, editor) {
  const fit = editor.artworkFit === 'cover' ? Math.max(width / imageWidth, height / imageHeight) : Math.min(width / imageWidth, height / imageHeight);
  const w = imageWidth * fit * editor.artworkScale;
  const h = imageHeight * fit * editor.artworkScale;
  return { x: (width - w) * editor.artworkX / 100, y: (height - h) * editor.artworkY / 100, width: w, height: h };
}
