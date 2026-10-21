import { Price } from '@/shared/components/ui/Price.jsx';
import { useState } from 'react';
import { previewGeometry } from '@/features/custom-design/previewGeometry.js';

export default function BuilderPreview({ builder }) {
  const { form, config, artworkPreview, quote, quoteError } = builder;
  const fontFamily = form.fontCode === 'poppins' ? "'Poppins', system-ui, sans-serif" : 'var(--font-body)';
  const colour = form.colourCode === 'white' ? '#fff' : 'var(--brand-navy-700)';
  const geometry = previewGeometry(form.shapeCode, form.width, form.height);
  const [guides, setGuides] = useState(false);
  const shapeLabel = config.options?.find(option => option.group === 'Shape' && option.code === form.shapeCode)?.label || form.shapeCode;
  return (

        <section className="s-preview-area" aria-label="Sticker preview" aria-live="polite">
          <span className="s-eyebrow">Live preview</span>
          <div className="s-preview-stage"><div className={`s-sticker-preview s-shape-${geometry.shape} ${guides ? 'has-guides' : ''}`} data-shape={geometry.shape} style={{ fontFamily, color: colour, textAlign: form.textAlignment, aspectRatio: geometry.ratio, width: `${geometry.width}%`, height: `${geometry.height}%` }}>
            {artworkPreview && <img src={artworkPreview} alt="Selected artwork preview" />}
            <span>{form.customText || 'Your sticker'}</span>
          </div></div>
          <p>{shapeLabel} · {form.width} × {form.height} {config.measurementUnit} · {form.quantity} {Number(form.quantity) === 1 ? 'sticker' : 'stickers'}</p>
          <button className="s-button s-button-text" type="button" aria-pressed={guides} onClick={() => setGuides(value => !value)}>{guides ? 'Hide' : 'Show'} layout guides</button>
          {guides && <small>Inset guide is illustrative. Confirm production bleed in the final proof.</small>}
          {!geometry.exact && <p role="status">{geometry.shape === 'circle' ? 'A circle needs equal width and height. This preview shows your current dimensions.' : 'The final contour follows the approved artwork; this is a bounding-box preview.'}</p>}
          {quote ? <div className="s-builder-quote"><span>Custom item subtotal</span><Price value={quote.subtotal} currency={quote.currency} /><small>{Number(quote.quantity) === 1 ? '1 sticker' : `${quote.quantity} stickers · ${new Intl.NumberFormat('en-LK', { style: 'currency', currency: quote.currency }).format(quote.unitPrice)} each`} · delivery calculated at checkout</small></div> : <p className="s-error-text" role="status">{quoteError || 'Calculating server quote…'}</p>}
        </section>
  );
}
