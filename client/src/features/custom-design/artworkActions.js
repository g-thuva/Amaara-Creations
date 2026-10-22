import { customerError } from '@/shared/api/apiError.ts';

export function artworkError(error) {
  const status = error?.response?.status;
  const messages = Object.values(error?.response?.data?.errors || {}).flat();
  if (status === 413 || (status === 400 && messages.some(message => String(message).includes('Multipart body length limit'))))
    return 'Artwork is too large. Choose a smaller image and try again.';
  return customerError(error);
}

// Local to one builder. A mutation invalidates any older design read immediately.
export function createArtworkActions({ api, onPending, onAsset, onRemoved, onDesign, onNotice, onError, onProgress, onUploadPending }) {
  let generation = 0;
  let active = true;
  let busy = false;
  let transfer = null;
  const beginRead = () => ++generation;
  const isCurrent = (request) => active && request === generation;

  const run = async (resolveDesign, mutate, apply, message, kind = 'remove') => {
    if (!active || busy) return null;
    busy = true;
    const request = beginRead();
    const controller = new AbortController();
    transfer = { controller, kind };
    let designId;
    let persisted = false;
    onPending(true);
    if (kind === 'upload') { onProgress?.(null); onUploadPending?.(true); }
    onError(null);
    try {
      const design = await resolveDesign();
      if (!design || !isCurrent(request)) return null;
      designId = design.id;
      if (controller.signal.aborted) throw Object.assign(new Error('Upload stopped'), { code: 'ERR_CANCELED' });
      const result = await mutate(design.id, { signal: controller.signal, onProgress: value => { if (isCurrent(request) && !controller.signal.aborted) onProgress?.(value); } });
      if (!isCurrent(request)) return null;
      if (controller.signal.aborted) throw Object.assign(new Error('Upload stopped'), { code: 'ERR_CANCELED' });
      apply(design.id, result);
      persisted = true;
      onNotice(message);
      const refreshed = await api.getDesign(design.id);
      if (isCurrent(request)) onDesign(refreshed);
      return result;
    } catch (error) {
      if (isCurrent(request) && (controller.signal.aborted || error?.code === 'ERR_CANCELED' || error?.name === 'AbortError')) {
        onNotice('Transfer stopped. Checking saved artwork; a completed upload may already be saved.');
        if (designId) {
          try { const refreshed = await api.getDesign(designId); if (isCurrent(request)) onDesign(refreshed); }
          catch { if (isCurrent(request)) onError(new Error('Reload this design to check its saved artwork before trying again.'), false); }
        }
      } else if (isCurrent(request)) onError(error, persisted);
      return null;
    } finally {
      busy = false;
      transfer = null;
      if (kind === 'upload') onUploadPending?.(false);
      if (active) onPending(false);
    }
  };

  return {
    beginRead,
    isCurrent,
    activate: () => { active = true; },
    dispose: () => { active = false; ++generation; transfer?.controller.abort(); },
    cancelUpload: () => { if (transfer?.kind === 'upload') transfer.controller.abort(); },
    upload: (resolveDesign, file) => run(resolveDesign,
      (id, options) => api.uploadArtwork(id, file, options),
      (id, asset) => {
        if (!asset?.id) throw new Error('The upload did not return a persisted artwork asset.');
        onAsset(id, asset, file);
      }, 'Artwork uploaded securely.', 'upload'),
    remove: (design, assetId) => run(() => design,
      (id) => api.deleteArtwork(id, assetId),
      (id) => onRemoved(id, assetId), 'Artwork removed.'),
  };
}
