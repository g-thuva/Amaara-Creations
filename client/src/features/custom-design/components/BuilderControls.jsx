import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { optionField } from '@/features/custom-design/builderDraft.js';
import { readEditorState } from '@/features/custom-design/editorState.js';

export default function BuilderControls({ builder }) {
  const { config, form, groups, change, busy, quote, design, save, upload, removeArtwork, addToCart } = builder;
  const editor = readEditorState(form.editor);
  return (
        <form className="s-form s-builder-controls" onSubmit={(event) => event.preventDefault()}>
          <h2>Design specification</h2>
          <p className="s-meta" role="status">{builder.draftStatus || (design ? 'Saved to your account. Save changes when ready.' : '')}</p>
          <label>Design name<input value={form.name} maxLength="160" required onChange={(event) => change('name', event.target.value)} /></label>
          <div className="s-form-grid">
            <label>Width ({config.measurementUnit})<input type="number" min={config.minimumWidth} max={config.maximumWidth} step={config.widthStep} value={form.width} onChange={(event) => change('width', event.target.value)} /></label>
            <label>Height ({config.measurementUnit})<input type="number" min={config.minimumHeight} max={config.maximumHeight} step={config.heightStep} value={form.height} onChange={(event) => change('height', event.target.value)} /></label>
            <label>Quantity<input type="number" min={config.minimumQuantity} max={config.maximumQuantity} step={config.quantityStep} value={form.quantity} onChange={(event) => change('quantity', event.target.value)} /></label>
          </div>
          {Object.entries(optionField).map(([group, field]) => groups[group]?.length ? (
            <label key={group}>{group}<select value={form[field]} onChange={(event) => change(field, event.target.value)}>{groups[group].map((option) => <option key={option.code} value={option.code}>{option.label}</option>)}</select></label>
          ) : null)}
          <label>Sticker text (optional)<input value={form.customText} maxLength="160" onChange={(event) => change('customText', event.target.value)} /></label>
          <label>Text alignment<select value={form.textAlignment} onChange={(event) => change('textAlignment', event.target.value)}><option value="left">Left</option><option value="center">Centre</option><option value="right">Right</option></select></label>
          <details><summary>Adjust text and artwork</summary><div className="s-form">
            <label>Preview background<select value={editor.background} onChange={event => builder.changeEditor('background', event.target.value)}><option value="white">White</option><option value="transparent">Transparent</option></select></label>
            <label>Artwork fit<select value={editor.artworkFit} onChange={event => builder.changeEditor('artworkFit', event.target.value)}><option value="contain">Fit entire image</option><option value="cover">Fill and crop</option></select></label>
            {Object.entries({ artworkScale: ['Artwork zoom', .25, 4, .05], artworkX: ['Artwork horizontal position', 0, 100, 1], artworkY: ['Artwork vertical position', 0, 100, 1], artworkRotation: ['Artwork rotation', -180, 180, 1], textSize: ['Text size', 2, 40, 1], textX: ['Text horizontal position', 0, 100, 1], textY: ['Text vertical position', 0, 100, 1] }).map(([field, [label, min, max, step]]) => <label key={field}>{label}<input type="range" min={min} max={max} step={step} value={editor[field]} onChange={event => builder.changeEditor(field, Number(event.target.value))} /><output aria-hidden="true">{editor[field]}{field === 'artworkRotation' ? '°' : field === 'artworkScale' ? '×' : '%'}</output></label>)}
            <Button variant="text" type="button" onClick={() => builder.change('editor', readEditorState(null))}>Reset layout adjustments</Button>
          </div></details>
          <label>Artwork (JPEG, PNG or WebP)<input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={upload} /></label>
          {builder.uploadingArtwork && <div role="status"><progress aria-label="Artwork transfer" max={100} value={builder.uploadProgress ?? undefined} /><p>{builder.uploadProgress === 100 ? 'Transfer sent. Validating and saving artwork…' : builder.uploadProgress == null ? 'Sending artwork…' : `Sending artwork: ${builder.uploadProgress}%`}</p><Button type="button" variant="secondary" onClick={builder.cancelUpload}>Stop upload</Button></div>}
          {builder.retryArtwork && !busy && <Button type="button" variant="secondary" onClick={builder.retryArtwork}>Retry artwork upload</Button>}
          {config.artworkRequired && <p className="s-meta">Artwork is required before this design can be added to the cart.</p>}
          {design?.assets?.map((asset) => <div className="s-row" key={asset.id}><span>{asset.originalFileName}</span><Button type="button" variant="text" disabled={busy} onClick={() => removeArtwork(asset.id)}>Remove</Button></div>)}
          <div className="s-actions"><Button type="button" variant="secondary" disabled={busy || !quote} onClick={save}>{busy ? 'Saving…' : 'Save design'}</Button><Button type="button" disabled={busy || !quote} onClick={addToCart}>Save and add to cart</Button></div>
          <p className="s-meta">Your quote updates with your options. The current order total is confirmed at checkout.</p>
          <Link className="s-text-link" to="/account/designs">View saved designs <Icon name="arrow" /></Link>
        </form>
  );
}
