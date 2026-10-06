import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Button, ErrorState, Icon, PageHeader, Price, Skeleton } from '../components/storefront/UI';
import { useAuth } from '../contexts/AuthContext';
import { useStore } from '../contexts/StoreContext';
import { customBuilderApi } from '../services/customBuilderApi';
import { customerError } from '../utils/storefront';
import { customDesignRequest, optionsByGroup } from '../utils/customBuilder';

const draftKey = 'amaara-custom-builder-draft';
const blank = { name: 'My custom sticker', width: '', height: '', quantity: '', shapeCode: '', materialCode: '', finishCode: '', fontCode: '', colourCode: '', customText: '', textAlignment: 'center', rowVersion: null };
const optionField = { Shape: 'shapeCode', Material: 'materialCode', Finish: 'finishCode', Font: 'fontCode', Colour: 'colourCode' };

export default function CustomBuilder() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const { cart, notify, requireLogin } = useStore();
  const [config, setConfig] = useState(null);
  const [form, setForm] = useState(blank);
  const [design, setDesign] = useState(null);
  const [quote, setQuote] = useState(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(null);
  const [quoteError, setQuoteError] = useState('');
  const [artworkPreview, setArtworkPreview] = useState('');
  const requestNumber = useRef(0);
  const groups = useMemo(() => optionsByGroup(config?.options), [config]);

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true); setError(null);
      try {
        const [configuration, saved] = await Promise.all([customBuilderApi.getConfig(), id ? customBuilderApi.getDesign(id) : Promise.resolve(null)]);
        if (!active) return;
        setConfig(configuration); setDesign(saved);
        const restored = !id ? JSON.parse(sessionStorage.getItem(draftKey) || 'null') : null;
        const source = saved || restored;
        const next = source ? {
          name: source.name || blank.name, width: source.width, height: source.height, quantity: source.quantity,
          shapeCode: source.shapeCode || '', materialCode: source.materialCode || '', finishCode: source.finishCode || '',
          fontCode: source.fontCode || '', colourCode: source.colourCode || '', customText: source.customText || '',
          textAlignment: source.textAlignment || 'center', rowVersion: source.rowVersion || null,
        } : {
          ...blank, width: configuration.minimumWidth, height: configuration.minimumHeight, quantity: configuration.minimumQuantity,
        };
        for (const [group, field] of Object.entries(optionField)) next[field] ||= configuration.options.find((option) => option.group === group && option.isActive)?.code || '';
        setForm(next);
      } catch (loadError) { if (active) setError(loadError); }
      finally { if (active) setLoading(false); }
    };
    load();
    return () => { active = false; };
  }, [id]);

  useEffect(() => {
    if (!config || loading) return undefined;
    const current = ++requestNumber.current;
    const controller = new AbortController();
    const timer = window.setTimeout(async () => {
      try {
        const result = await customBuilderApi.quote(customDesignRequest(form), controller.signal);
        if (current === requestNumber.current) { setQuote(result); setQuoteError(''); }
      } catch (quoteFailure) {
        if (quoteFailure?.code !== 'ERR_CANCELED' && current === requestNumber.current) { setQuote(null); setQuoteError(customerError(quoteFailure, 'Choose a valid configuration to receive a quote.')); }
      }
    }, 350);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [config, form, loading]);

  useEffect(() => () => { if (artworkPreview) URL.revokeObjectURL(artworkPreview); }, [artworkPreview]);

  const change = (field, value) => setForm((current) => ({ ...current, [field]: value }));

  const save = async () => {
    if (!isAuthenticated) {
      sessionStorage.setItem(draftKey, JSON.stringify(form));
      requireLogin();
      return null;
    }
    setPending(true); setError(null);
    try {
      const saved = design?.id ? await customBuilderApi.updateDesign(design.id, customDesignRequest(form)) : await customBuilderApi.createDesign(customDesignRequest(form));
      setDesign(saved); setForm((current) => ({ ...current, rowVersion: saved.rowVersion }));
      sessionStorage.removeItem(draftKey);
      if (!design?.id) navigate(`/custom/${saved.id}`, { replace: true });
      notify('Custom design saved.');
      return saved;
    } catch (saveError) { setError(saveError); return null; }
    finally { setPending(false); }
  };

  const upload = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) { setQuoteError('Artwork must be a JPEG, PNG or WebP image.'); return; }
    const saved = design?.id ? design : await save();
    if (!saved) return;
    if (artworkPreview) URL.revokeObjectURL(artworkPreview);
    setArtworkPreview(URL.createObjectURL(file)); setPending(true);
    try {
      await customBuilderApi.uploadArtwork(saved.id, file);
      const refreshed = await customBuilderApi.getDesign(saved.id);
      setDesign(refreshed);
      setForm((current) => ({ ...current, rowVersion: refreshed.rowVersion }));
      notify('Artwork uploaded securely.');
    } catch (uploadError) { setError(uploadError); }
    finally { setPending(false); event.target.value = ''; }
  };

  const removeArtwork = async (assetId) => {
    setPending(true);
    try {
      await customBuilderApi.deleteArtwork(design.id, assetId);
      const refreshed = await customBuilderApi.getDesign(design.id);
      setDesign(refreshed);
      setForm((current) => ({ ...current, rowVersion: refreshed.rowVersion }));
      notify('Artwork removed.');
    }
    catch (removeError) { setError(removeError); }
    finally { setPending(false); }
  };

  const addToCart = async () => {
    const saved = await save();
    if (!saved) return;
    setPending(true);
    try { await customBuilderApi.addToCart(saved.id); await cart.reload(); notify('Custom design added to your cart.'); navigate('/cart'); }
    catch (cartError) { setError(cartError); }
    finally { setPending(false); }
  };

  if (loading) return <div className="s-container s-page"><Skeleton count={3} /></div>;
  if (error && !config) return <div className="s-container s-page"><ErrorState error={error} retry={() => navigate(0)} /></div>;

  const fontFamily = form.fontCode === 'poppins' ? 'var(--font-heading)' : 'var(--font-body)';
  const colour = form.colourCode === 'white' ? '#fff' : 'var(--brand-navy-700)';
  return (
    <div className="s-container s-page s-custom-page">
      <PageHeader eyebrow="Custom sticker builder" title={design ? `Edit ${design.name}` : 'Build a custom sticker'}>
        Configure a sticker, receive a server-calculated item quote, upload private artwork and save the design to your account.
      </PageHeader>
      {error && <p className="s-error-text" role="alert">{customerError(error)}</p>}
      <div className="s-builder">
        <form className="s-form s-builder-controls" onSubmit={(event) => event.preventDefault()}>
          <h2>Design specification</h2>
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
          <label>Artwork (JPEG, PNG or WebP)<input type="file" accept="image/jpeg,image/png,image/webp" disabled={pending} onChange={upload} /></label>
          {config.artworkRequired && <p className="s-meta">Artwork is required before this design can be added to the cart.</p>}
          {design?.assets?.map((asset) => <div className="s-row" key={asset.id}><span>{asset.originalFileName}</span><Button type="button" variant="text" disabled={pending} onClick={() => removeArtwork(asset.id)}>Remove</Button></div>)}
          <div className="s-actions"><Button type="button" variant="secondary" disabled={pending || !quote} onClick={save}>{pending ? 'Saving…' : 'Save design'}</Button><Button type="button" disabled={pending || !quote} onClick={addToCart}>Save and add to cart</Button></div>
          <p className="s-meta">The server validates every option and recalculates the price when saving, adding to cart and creating an order.</p>
          <Link className="s-text-link" to="/account/designs">View saved designs <Icon name="arrow" /></Link>
        </form>
        <section className="s-preview-area" aria-label="Sticker preview" aria-live="polite">
          <span className="s-eyebrow">Live preview</span>
          <div className="s-sticker-preview" style={{ fontFamily, color: colour, textAlign: form.textAlignment, aspectRatio: `${Number(form.width) || 1} / ${Number(form.height) || 1}` }}>
            {artworkPreview && <img src={artworkPreview} alt="Selected artwork preview" />}
            <span>{form.customText || 'Your sticker'}</span>
          </div>
          <p>{form.width} × {form.height} {config.measurementUnit} · {form.quantity} stickers</p>
          {quote ? <div className="s-builder-quote"><span>Custom item subtotal</span><Price value={quote.subtotal} /><small>{quote.quantity} × <Price value={quote.unitPrice} /> · shipping and payment not included</small></div> : <p className="s-error-text" role="status">{quoteError || 'Calculating server quote…'}</p>}
        </section>
      </div>
      <div className="s-builder-sticky"><span>{quote ? <Price value={quote.subtotal} /> : 'Quote required'}</span><Button type="button" disabled={pending || !quote} onClick={addToCart}>Add custom design</Button></div>
    </div>
  );
}
