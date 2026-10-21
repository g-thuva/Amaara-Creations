import { artworkPlacement, readEditorState } from './editorState.js';
import { previewGeometry } from './previewGeometry.js';

export const previewFonts = { inter: 'Inter', poppins: 'Poppins' };
export const previewColours = { navy: '#102744', black: '#111111', white: '#ffffff', blue: '#2a75bf', red: '#b82129' };
export function layoutColour(code) { return previewColours[code] || (/^#[\da-f]{6}$/i.test(code || '') ? code : '#102744'); }
export function shapePath(context, shape, width, height) {
  context.beginPath();
  if (['circle', 'oval'].includes(shape)) context.ellipse(width / 2, height / 2, width / 2, height / 2, 0, 0, Math.PI * 2);
  else if (shape === 'rounded') context.roundRect(0, 0, width, height, Math.min(width, height) * .12);
  else context.rect(0, 0, width, height);
}

export async function downloadLayout(form, artworkUrl, unit) {
  const widthCm = Number(form.width); const heightCm = Number(form.height);
  if (unit !== 'cm' || widthCm <= 0 || heightCm <= 0) throw new Error('A preview download requires valid dimensions in cm.');
  const width = Math.round(widthCm / 2.54 * 300); const height = Math.round(heightCm / 2.54 * 300);
  if (width < 1 || height < 1 || width * height > 24_000_000) throw new Error('This layout is too large to download in the browser. Ask for a proof instead.');
  await document.fonts.ready;
  const editor = readEditorState(form.editor);
  const canvas = document.createElement('canvas'); canvas.width = width; canvas.height = height;
  const context = canvas.getContext('2d');
  shapePath(context, previewGeometry(form.shapeCode, widthCm, heightCm).shape, width, height); context.clip();
  if (editor.background === 'white') { context.fillStyle = '#fff'; context.fillRect(0, 0, width, height); }
  if (artworkUrl) {
    const image = new Image(); image.src = artworkUrl; await image.decode();
    const placement = artworkPlacement(image.naturalWidth, image.naturalHeight, width, height, editor);
    context.save(); context.translate(placement.x + placement.width / 2, placement.y + placement.height / 2); context.rotate(editor.artworkRotation * Math.PI / 180);
    context.drawImage(image, -placement.width / 2, -placement.height / 2, placement.width, placement.height); context.restore();
  }
  const size = width * editor.textSize / 100;
  context.font = `${form.fontCode === 'poppins' ? 600 : 400} ${size}px "${previewFonts[form.fontCode] || 'Inter'}"`;
  context.fillStyle = layoutColour(form.colourCode); context.textAlign = form.textAlignment; context.textBaseline = 'middle';
  const words = String(form.customText || '').split(/\s+/); const lines = []; let line = '';
  for (const word of words) {
    const next = line ? `${line} ${word}` : word;
    if (line && context.measureText(next).width > width * .9) { lines.push(line); line = word; } else line = next;
  }
  if (line) lines.push(line);
  const x = width * editor.textX / 100 + (form.textAlignment === 'left' ? -width * .45 : form.textAlignment === 'right' ? width * .45 : 0);
  lines.forEach((text, index) => context.fillText(text, x, height * editor.textY / 100 + (index - (lines.length - 1) / 2) * size * 1.15, width * .9));
  const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
  if (!blob) throw new Error('The layout preview could not be created. Try again.');
  const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url;
  link.download = `${String(form.name || 'sticker').replace(/[^a-z\d _-]/gi, '').slice(0, 60) || 'sticker'}-layout-preview.png`;
  document.body.append(link); link.click(); link.remove(); window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}
