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
import { changePreviewSpecification } from '@/features/custom-design/previewGeometry.js';
import { defaultEditor, readEditorState } from '@/features/custom-design/editorState.js';

export function useCustomBuilder(id) {
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();
  const { cart } = useCart();
  const { notify } = useToast();
  const requireLogin = useRequireLogin();
  const [config, setConfig] = useState(null);
  const [form, setForm] = useState(blank);
  const [design, setDesign] = useState(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [artworkPending, setArtworkPending] = useState(false);
  const [uploadingArtwork, setUploadingArtwork] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(null);
  const [retryFile, setRetryFile] = useState(null);
  const [error, setError] = useState(null);
  const [artworkPreview, setArtworkPreview] = useState('');
  const [draftStatus, setDraftStatus] = useState('');
  const [draftConflict, setDraftConflict] = useState(null);
  const draftKey = id ? `${user?.id}:${id}` : null;
  const loadedDesignId = useRef(undefined);
  const saving = useRef(false);
  const adding = useRef(false);
  const [artworkActions] = useState(() => createArtworkActions({
    api: customDesignApi,
    onPending: setArtworkPending,
    onUploadPending: setUploadingArtwork,
    onProgress: setUploadProgress,
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
  const busy = pending || artworkPending || Boolean(draftConflict);

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
        const restored = readBuilderDraft(undefined, draftKey);
        const conflict = Boolean(saved && restored && restored.baseUpdatedAt !== saved.updatedAt);
        setDraftConflict(conflict ? restored : null);
        const source = (!conflict && restored) || saved;
        const next = source ? {
          name: source.name || blank.name, width: source.width, height: source.height, quantity: source.quantity,
          shapeCode: source.shapeCode || '', materialCode: source.materialCode || '', finishCode: source.finishCode || '',
          fontCode: source.fontCode || '', colourCode: source.colourCode || '', customText: source.customText || '',
          textAlignment: source.textAlignment || 'center', rowVersion: source.rowVersion || null,
          editor: readEditorState(source.editorStateJson || source.editor),
        } : {
          ...blank, editor: { ...defaultEditor }, width: configuration.minimumWidth, height: configuration.minimumHeight, quantity: configuration.minimumQuantity,
        };
        for (const [group, field] of Object.entries(optionField)) next[field] ||= configuration.options.find((option) => option.group === group && option.isActive)?.code || '';
        setForm(next);
      } catch (loadError) { if (active && artworkActions.isCurrent(request)) setError(loadError); }
      finally { if (active && artworkActions.isCurrent(request)) setLoading(false); }
    };
    load();
    return () => { active = false; controller.abort(); };
  }, [id, artworkActions, draftKey]);

  const { quote, quoteError } = useBuilderQuote(config, form, loading);

  useEffect(() => {
    if (loading || draftConflict) return undefined;
    setDraftStatus('Saving draft on this device…');
    const timer = window.setTimeout(() => {
      const saved = saveBuilderDraft({ ...form, baseUpdatedAt: design?.updatedAt }, undefined, draftKey);
      setDraftStatus(saved ? 'Draft saved for this browser session. Artwork is saved separately to your account.' : 'Device storage is unavailable. Save to your account to keep this design.');
    }, 600);
    return () => window.clearTimeout(timer);
  }, [form, loading, draftKey, draftConflict, design?.updatedAt]);

  useEffect(() => {
    if (!design?.assets?.length) return undefined;
    const asset = design.assets[design.assets.length - 1];
    const url = asset.downloadUrl;
    if (!url) return undefined;
    let active = true; let objectUrl;
    customDesignApi.downloadPrivate(url).then(blob => {
      if (!active) return;
      objectUrl = URL.createObjectURL(blob); setArtworkPreview(objectUrl);
    }).catch(() => { if (active) setError(new Error('Unable to restore the private artwork preview. Retry opening this design.')); });
    return () => { active = false; if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [design?.id, design?.assets]);

  useEffect(() => () => { if (artworkPreview) URL.revokeObjectURL(artworkPreview); }, [artworkPreview]);

  const change = (field, value) => setForm((current) => changePreviewSpecification(current, field, value));
  const changeEditor = (field, value) => setForm(current => ({ ...current, editor: readEditorState({ ...current.editor, [field]: value }) }));

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
      clearBuilderDraft(); clearBuilderDraft(undefined, draftKey);
      if (!design?.id) navigate(`/custom/${saved.id}`, { replace: true });
      notify('Custom design saved.');
      return saved;
    } catch (saveError) {
      setError(saveError);
      if (saveError?.response?.status === 409) {
        saveBuilderDraft({ ...form, baseUpdatedAt: design?.updatedAt }, undefined, draftKey);
        try { const latest = await customDesignApi.getDesign(design.id); setDesign(latest); setForm(current => ({ ...current, rowVersion: latest.rowVersion })); }
        catch { /* Keep local edits recoverable if the latest read also fails. */ }
        setDraftConflict(form);
      }
      return null;
    }
    finally { saving.current = false; setPending(false); }
  };

  const uploadFile = async file => {
    setRetryFile(null);
    const result = await artworkActions.upload(() => design?.id ? design : save(), file);
    setRetryFile(result ? null : file);
  };
  const upload = async (event) => {
    const input = event.target;
    const file = input.files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      const failure = { response: { status: 400, data: { errors: { file: ['Artwork must be a JPEG, PNG or WebP image.'] } } } };
      setError(failure); notify(customerError(failure), 'error'); input.value = ''; return;
    }
    await uploadFile(file);
    input.value = '';
  };

  const removeArtwork = (assetId) => { setRetryFile(null); return artworkActions.remove(design, assetId); };

  const discardDraft = () => { clearBuilderDraft(undefined, draftKey); navigate(0); };
  const recoverDraft = () => { setForm(current => ({ ...draftConflict, rowVersion: current.rowVersion, editor: readEditorState(draftConflict.editor) })); setDraftConflict(null); setDraftStatus('Recovered local edits. Review them, then save against the current account version.'); };

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


  return { config, form, design, quote, loading, busy, error, quoteError, draftStatus, draftConflict, recoverDraft, discardDraft, artworkPreview, groups, change, changeEditor, save, upload, removeArtwork, uploadingArtwork, uploadProgress, cancelUpload: artworkActions.cancelUpload, retryArtwork: retryFile ? () => uploadFile(retryFile) : null, addToCart, reload: () => navigate(0) };
}
