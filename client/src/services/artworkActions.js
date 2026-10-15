import { customerError } from '../utils/storefront';

export function artworkError(error) {
  const status = error?.response?.status;
  const messages = Object.values(error?.response?.data?.errors || {}).flat();
  if (status === 413 || (status === 400 && messages.some(message => String(message).includes('Multipart body length limit'))))
    return 'Artwork is too large. Choose a smaller image and try again.';
  return customerError(error);
}

// Local to one builder. A mutation invalidates any older design read immediately.
export function createArtworkActions({ api, onPending, onAsset, onRemoved, onDesign, onNotice, onError }) {
  let generation = 0;
  let active = true;
  let busy = false;
  const beginRead = () => ++generation;
  const isCurrent = (request) => active && request === generation;

  const run = async (resolveDesign, mutate, apply, message) => {
    if (!active || busy) return null;
    busy = true;
    const request = beginRead();
    let persisted = false;
    onPending(true);
    onError(null);
    try {
      const design = await resolveDesign();
      if (!design || !isCurrent(request)) return null;
      const result = await mutate(design.id);
      if (!isCurrent(request)) return null;
      apply(design.id, result);
      persisted = true;
      onNotice(message);
      const refreshed = await api.getDesign(design.id);
      if (isCurrent(request)) onDesign(refreshed);
      return result;
    } catch (error) {
      if (isCurrent(request)) onError(error, persisted);
      return null;
    } finally {
      busy = false;
      if (active) onPending(false);
    }
  };

  return {
    beginRead,
    isCurrent,
    activate: () => { active = true; },
    dispose: () => { active = false; ++generation; },
    upload: (resolveDesign, file) => run(resolveDesign,
      (id) => api.uploadArtwork(id, file),
      (id, asset) => {
        if (!asset?.id) throw new Error('The upload did not return a persisted artwork asset.');
        onAsset(id, asset, file);
      }, 'Artwork uploaded securely.'),
    remove: (design, assetId) => run(() => design,
      (id) => api.deleteArtwork(id, assetId),
      (id) => onRemoved(id, assetId), 'Artwork removed.'),
  };
}
