import { Price } from '@/shared/components/ui/Price.jsx';

export default function BuilderPreview({ builder }) {
  const { form, config, artworkPreview, quote, quoteError } = builder;
  const fontFamily = form.fontCode === 'poppins' ? 'var(--font-heading)' : 'var(--font-body)';
  const colour = form.colourCode === 'white' ? '#fff' : 'var(--brand-navy-700)';
  return (

        <section className="s-preview-area" aria-label="Sticker preview" aria-live="polite">
          <span className="s-eyebrow">Live preview</span>
          <div className="s-sticker-preview" style={{ fontFamily, color: colour, textAlign: form.textAlignment, aspectRatio: `${Number(form.width) || 1} / ${Number(form.height) || 1}` }}>
            {artworkPreview && <img src={artworkPreview} alt="Selected artwork preview" />}
            <span>{form.customText || 'Your sticker'}</span>
          </div>
          <p>{form.width} × {form.height} {config.measurementUnit} · {form.quantity} stickers</p>
          {quote ? <div className="s-builder-quote"><span>Custom item subtotal</span><Price value={quote.subtotal} currency={quote.currency} /><small>{quote.quantity} × <Price value={quote.unitPrice} currency={quote.currency} /> · shipping and payment not included</small></div> : <p className="s-error-text" role="status">{quoteError || 'Calculating server quote…'}</p>}
        </section>
  );
}
