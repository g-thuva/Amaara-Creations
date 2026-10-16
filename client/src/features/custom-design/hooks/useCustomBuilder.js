import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { useRequireLogin } from '@/features/auth/hooks/useRequireLogin.js';
import { useCart } from '@/features/cart/CartProvider.jsx';
import { useToast } from '@/shared/components/feedback/ToastProvider.jsx';
import { customerError } from '@/shared/api/apiError.ts';
import { customDesignApi } from '@/features/custom-design/customDesignApi.js';
import { artworkError, createArtworkActions } from '@/features/custom-design/artworkActions.js';
import { customDesignRequest, optionsByGroup } from '@/features/custom-design/designModel.js';
import { blank, optionField, readBuilderDraft, saveBuilderDraft, clearBuilderDraft } from '@/features/custom-design/builderDraft.js';
import { useBuilderQuote } from '@/features/custom-design/hooks/useBuilderQuote.js';

export function useCustomBuilder(id) {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const { cart } = useCart();
  const { notify } = useToast();
  const requireLogin = useRequireLogin();
  const [config, setConfig] = useState(null);
  const [form, setForm] = useState(blank);
  const [design, setDesign] = useState(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [artworkPending, setArtworkPending] = useState(false);
  const [error, setError] = useState(null);
  const [artworkPreview, setArtworkPreview] = useState('');
  const loadedDesignId = useRef(undefined);
  const saving = useRef(false);
  const adding = useRef(false);
  const [artworkActions] = useState(() => createArtworkActions({
    api: customDesignApi,
    onPending: setArtworkPending,
    onAsset: (designId, asset, file) => {
      setDesign((current) => current?.id === designId ? { ...current, assets: [...(current.assets || []), asset] } : current);
      setArtworkPreview(URL.createObjectURL(file));
    },
    onRemoved: (designId, assetId) => {
      setDesign((current) => current?.id === designId ? { ...current, assets: current.assets.filter((asset) => asset.id !== assetId) } : current);
      setArtworkPreview('');
    },
    onDesign: (refreshed) => {
      setDesign((current) => current?.id === refreshed.id ? refreshed : current);
      setForm((current) => ({ ...current, rowVersion: refreshed.rowVersion }));
    },
    onNotice: notify,
    onError: (failure, persisted) => {
      setError(failure);
      if (failure && !persisted) notify(artworkError(failure), 'error');
    },
  }));
  const groups = useMemo(() => optionsByGroup(config?.options), [config]);
  const busy = pending || artworkPending;

  useEffect(() => {
    artworkActions.activate();
    return () => artworkActions.dispose();
  }, [artworkActions]);

  useEffect(() => {
    // Create already returned this authoritative design. Keep the input mounted
    // when the URL changes, including an upload that saved its design first.
    if (id && Number(id) === loadedDesignId.current) return undefined;
    let active = true;
    const request = artworkActions.beginRead();
    const controller = new AbortController();
    const load = async () => {
      setLoading(true); setError(null); setArtworkPreview('');
      try {
        const [configuration, saved] = await Promise.all([customDesignApi.getConfig(controller.signal), id ? customDesignApi.getDesign(id, controller.signal) : Promise.resolve(null)]);
        if (!active || !artworkActions.isCurrent(request)) return;
        loadedDesignId.current = saved?.id ?? null;
        setConfig(configuration); setDesign(saved);
        const restored = !id ? readBuilderDraft() : null;
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
      } catch (loadError) { if (active && artworkActions.isCurrent(request)) setError(loadError); }
      finally { if (active && artworkActions.isCurrent(request)) setLoading(false); }
    };
    load();
    return () => { active = false; controller.abort(); };
  }, [id, artworkActions]);

  const { quote, quoteError } = useBuilderQuote(config, form, loading);

  useEffect(() => () => { if (artworkPreview) URL.revokeObjectURL(artworkPreview); }, [artworkPreview]);

  const change = (field, value) => setForm((current) => ({ ...current, [field]: value }));

  const save = async () => {
    if (saving.current) return null;
    if (!isAuthenticated) {
      saveBuilderDraft(form);
      requireLogin();
      return null;
    }
    saving.current = true;
    setPending(true); setError(null);
    try {
      const saved = design?.id ? await customDesignApi.updateDesign(design.id, customDesignRequest(form)) : await customDesignApi.createDesign(customDesignRequest(form));
      loadedDesignId.current = saved.id;
      setDesign(saved); setForm((current) => ({ ...current, rowVersion: saved.rowVersion }));
      clearBuilderDraft();
      if (!design?.id) navigate(`/custom/${saved.id}`, { replace: true });
      notify('Custom design saved.');
      return saved;
    } catch (saveError) { setError(saveError); return null; }
    finally { saving.current = false; setPending(false); }
  };

  const upload = async (event) => {
    const input = event.target;
    const file = input.files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      const failure = { response: { status: 400, data: { errors: { file: ['Artwork must be a JPEG, PNG or WebP image.'] } } } };
      setError(failure); notify(customerError(failure), 'error'); input.value = ''; return;
    }
    await artworkActions.upload(() => design?.id ? design : save(), file);
    input.value = '';
  };

  const removeArtwork = (assetId) => artworkActions.remove(design, assetId);

  const addToCart = async () => {
    if (adding.current) return;
    adding.current = true;
    const saved = await save();
    if (!saved) { adding.current = false; return; }
    setPending(true);
    try { await customDesignApi.addToCart(saved.id); await cart.reload(); notify('Custom design added to your cart.'); navigate('/cart'); }
    catch (cartError) { setError(cartError); }
    finally { adding.current = false; setPending(false); }
  };


  return { config, form, design, quote, loading, busy, error, quoteError, artworkPreview, groups, change, save, upload, removeArtwork, addToCart, reload: () => navigate(0) };
}
