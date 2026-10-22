import { Price } from '@/shared/components/ui/Price.jsx';
import { useState } from 'react';
import { previewGeometry } from '@/features/custom-design/previewGeometry.js';
import { artworkPlacement, readEditorState } from '@/features/custom-design/editorState.js';
import { downloadLayout, layoutColour, previewColours, previewFonts } from '@/features/custom-design/renderLayout.js';

export default function BuilderPreview({ builder }) {
  const { form, config, artworkPreview, quote, quoteError } = builder;
  const fontFamily = form.fontCode === 'poppins' ? "'Poppins', system-ui, sans-serif" : 'var(--font-body)';
  const colour = layoutColour(form.colourCode);
  const geometry = previewGeometry(form.shapeCode, form.width, form.height);
  const [guides, setGuides] = useState(false);
  const [imageSize, setImageSize] = useState(null);
  const [downloadError, setDownloadError] = useState('');
  const [downloading, setDownloading] = useState(false);
  const editor = readEditorState(form.editor);
  const placement = imageSize ? artworkPlacement(imageSize.width, imageSize.height, 100, 100 / geometry.ratio, editor) : null;
  const dimensions = builder.design?.assets?.at(-1);
  const sourceWidth = imageSize?.width || dimensions?.width;
  const sourceHeight = imageSize?.height || dimensions?.height;
  const ppi = sourceWidth && sourceHeight && config.measurementUnit === 'cm' ? (editor.artworkFit === 'cover' ? Math.min : Math.max)(sourceWidth / (Number(form.width) / 2.54), sourceHeight / (Number(form.height) / 2.54)) / editor.artworkScale : null;
  async function exportPreview() {
    setDownloading(true); setDownloadError('');
    try { await downloadLayout(form, artworkPreview, config.measurementUnit); }
    catch (error) { setDownloadError(error.message); }
    finally { setDownloading(false); }
  }
  const shapeLabel = config.options?.find(option => option.group === 'Shape' && option.code === form.shapeCode)?.label || form.shapeCode;
  return (

        <section className="s-preview-area" aria-label="Sticker preview" aria-live="polite">
          <span className="s-eyebrow">Live preview</span>
          <div className="s-preview-stage"><div className={`s-sticker-preview s-shape-${geometry.shape} ${guides ? 'has-guides' : ''}`} data-shape={geometry.shape} style={{ fontFamily, fontWeight: form.fontCode === 'poppins' ? 600 : 400, color: colour, textAlign: form.textAlignment, background: editor.background === 'transparent' ? 'transparent' : '#fff', aspectRatio: geometry.ratio, width: `${geometry.width}%`, height: `${geometry.height}%` }}>
            {artworkPreview && <img src={artworkPreview} alt="Selected artwork preview" onLoad={event => setImageSize({ width: event.currentTarget.naturalWidth, height: event.currentTarget.naturalHeight })} style={placement ? { position: 'absolute', width: `${placement.width}%`, height: `${placement.height * geometry.ratio}%`, left: `${placement.x}%`, top: `${placement.y * geometry.ratio}%`, maxWidth: 'none', objectFit: 'fill', transform: `rotate(${editor.artworkRotation}deg)` } : undefined} />}
            <span style={{ position: 'absolute', left: `${editor.textX}%`, top: `${editor.textY}%`, transform: 'translate(-50%, -50%)', fontSize: `${editor.textSize}cqw`, width: '90%', maxWidth: '90%', padding: 0 }}>{form.customText || 'Your sticker'}</span>
          </div></div>
          <p>{shapeLabel} · {form.width} × {form.height} {config.measurementUnit} · {form.quantity} {Number(form.quantity) === 1 ? 'sticker' : 'stickers'}</p>
          <button className="s-button s-button-text" type="button" aria-pressed={guides} onClick={() => setGuides(value => !value)}>{guides ? 'Hide' : 'Show'} layout guides</button>
          {guides && <small>Inset guide is illustrative. Confirm production bleed in the final proof.</small>}
          {artworkPreview && ppi && ppi < 150 && <p role="status" className="s-error-text">Artwork is approximately {Math.round(ppi)} pixels per inch at this size. Use a larger image or review sharpness in the proof.</p>}
          {(!previewFonts[form.fontCode] || (!previewColours[form.colourCode] && !/^#[\da-f]{6}$/i.test(form.colourCode || ''))) && <p className="s-meta">Some configured fonts or colours use a preview substitute. Confirm their exact appearance in your production proof.</p>}
          <button className="s-button s-button-secondary" type="button" disabled={downloading} onClick={exportPreview}>{downloading ? 'Preparing preview…' : 'Download layout preview'}</button>
          <small>PNG at your layout proportions. Guides are excluded. Production bleed and cut paths must be confirmed in the approved proof.</small>
          {downloadError && <p role="alert" className="s-error-text">{downloadError}</p>}
          {!geometry.exact && <p role="status">{geometry.shape === 'circle' ? 'A circle needs equal width and height. This preview shows your current dimensions.' : 'The final contour follows the approved artwork; this is a bounding-box preview.'}</p>}
          {quote ? <div className="s-builder-quote"><span>Custom item subtotal</span><Price value={quote.subtotal} currency={quote.currency} /><small>{Number(quote.quantity) === 1 ? '1 sticker' : `${quote.quantity} stickers · ${new Intl.NumberFormat('en-LK', { style: 'currency', currency: quote.currency }).format(quote.unitPrice)} each`} · delivery calculated at checkout</small></div> : <p className="s-error-text" role="status">{quoteError || 'Calculating server quote…'}</p>}
        </section>
  );
}
