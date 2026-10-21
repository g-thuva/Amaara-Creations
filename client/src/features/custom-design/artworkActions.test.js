import { describe, expect, it, vi } from 'vitest';
import { artworkError, createArtworkActions } from '@/features/custom-design/artworkActions.js';

const deferred = () => {
  let resolve, reject;
  const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
};
const asset = { id: 8, originalFileName: 'private.png', downloadUrl: '/api/v1/custom-designs/1/assets/8' };
function fixture(overrides = {}) {
  let design = { id: 1, assets: [] };
  const notices = [];
  const errors = [];
  const api = {
    uploadArtwork: vi.fn(async () => asset),
    deleteArtwork: vi.fn(async () => undefined),
    getDesign: vi.fn(async () => ({ id: 1, assets: [asset], rowVersion: 'server-version' })),
    ...overrides,
  };
  const pending = vi.fn();
  const actions = createArtworkActions({ api, onPending: pending,
    onAsset: (id, uploaded) => { design = { ...design, assets: [...design.assets, uploaded] }; },
    onRemoved: (id, assetId) => { design = { ...design, assets: design.assets.filter(item => item.id !== assetId) }; },
    onDesign: fresh => { design = fresh; },
    onNotice: text => notices.push(text), onError: (error, persisted) => { if (error) errors.push({ error, persisted }); },
  });
  return { actions, api, notices, errors, pending, get design() { return design; } };
}

describe('persisted private artwork actions', () => {
  it.each([413, 400])('gives actionable guidance for upload size rejection HTTP %i', status => {
    expect(artworkError({ response: { status, data: { errors: { file: ['Multipart body length limit 5242880 exceeded.'] } } } }))
      .toBe('Artwork is too large. Choose a smaller image and try again.');
  });
  it('keeps server internals out of an HTTP 500 upload error', () => {
    expect(artworkError({ response: { status: 500, data: { detail: 'private storage path' } } }))
      .toBe('We could not complete your request. Please try again.');
  });
  it('waits for upload success before announcing completion or adding an asset', async () => {
    const upload = deferred();
    const f = fixture({ uploadArtwork: vi.fn(() => upload.promise) });
    const action = f.actions.upload(() => f.design, {});
    await vi.waitFor(() => expect(f.api.uploadArtwork).toHaveBeenCalledOnce());
    expect(f.notices).toEqual([]); expect(f.design.assets).toEqual([]);
    upload.resolve(asset); await action;
    expect(f.notices).toEqual(['Artwork uploaded securely.']);
    expect(f.design.assets).toEqual([asset]);
  });
  it('renders the authoritative asset and notice while detail refresh is pending', async () => {
    const details = deferred();
    const f = fixture({ getDesign: vi.fn(() => details.promise) });
    const action = f.actions.upload(() => f.design, {});
    await vi.waitFor(() => expect(f.api.getDesign).toHaveBeenCalledOnce());
    expect(f.design.assets).toEqual([asset]); expect(f.notices).toEqual(['Artwork uploaded securely.']);
    expect(f.pending).toHaveBeenLastCalledWith(true);
    details.resolve({ id: 1, assets: [asset] }); await action;
    expect(f.notices).toEqual(['Artwork uploaded securely.']); expect(f.pending).toHaveBeenLastCalledWith(false);
  });
  it('reconciles row version and persisted artwork after refresh', async () => {
    const f = fixture(); await f.actions.upload(() => f.design, {});
    expect(f.design.rowVersion).toBe('server-version'); expect(f.design.assets).toEqual([asset]);
  });
  it('invalidates a stale older design read before it can remove newly uploaded artwork', async () => {
    const f = fixture(); const olderRead = f.actions.beginRead();
    await f.actions.upload(() => f.design, {});
    expect(f.actions.isCurrent(olderRead)).toBe(false); expect(f.design.assets).toEqual([asset]);
  });
  it.each([400, 500])('reports upload HTTP %i failure without success or changing existing artwork', async status => {
    const failure = { response: { status } };
    const f = fixture({ uploadArtwork: vi.fn(async () => { throw failure; }) });
    await f.actions.upload(() => f.design, {});
    expect(f.errors).toEqual([{ error: failure, persisted: false }]);
    expect(f.notices).toEqual([]); expect(f.design.assets).toEqual([]); expect(f.api.getDesign).not.toHaveBeenCalled();
    expect(f.pending).toHaveBeenLastCalledWith(false);
  });
  it('keeps a successful asset and notice if subsequent detail refresh fails', async () => {
    const f = fixture({ getDesign: vi.fn(async () => { throw { response: { status: 503 } }; }) });
    await f.actions.upload(() => f.design, {});
    expect(f.design.assets).toEqual([asset]); expect(f.notices).toEqual(['Artwork uploaded securely.']);
    expect(f.errors[0].persisted).toBe(true);
  });
  it('removes only after successful deletion and preserves the removal through refetch', async () => {
    const f = fixture(); await f.actions.upload(() => f.design, {});
    f.api.getDesign.mockResolvedValue({ id: 1, assets: [] });
    await f.actions.remove(f.design, asset.id);
    expect(f.api.deleteArtwork).toHaveBeenCalledWith(1, 8); expect(f.design.assets).toEqual([]);
    expect(f.notices).toEqual(['Artwork uploaded securely.', 'Artwork removed.']);
  });
  it('guards rapid duplicate upload actions while automatic save is pending', async () => {
    const save = deferred(); const f = fixture();
    const first = f.actions.upload(() => save.promise, {});
    expect(await f.actions.upload(() => f.design, {})).toBeNull();
    save.resolve(f.design); await first;
    expect(f.api.uploadArtwork).toHaveBeenCalledOnce(); expect(f.design.assets).toEqual([asset]);
  });
  it('ignores late upload results after another design starts loading', async () => {
    const upload = deferred(); const f = fixture({ uploadArtwork: vi.fn(() => upload.promise) });
    const action = f.actions.upload(() => f.design, {});
    await vi.waitFor(() => expect(f.api.uploadArtwork).toHaveBeenCalledOnce());
    f.actions.beginRead(); upload.resolve(asset); await action;
    expect(f.notices).toEqual([]); expect(f.design.assets).toEqual([]); expect(f.api.getDesign).not.toHaveBeenCalled();
  });
  it('does not announce an empty upload DTO as a persisted asset', async () => {
    const f = fixture({ uploadArtwork: vi.fn(async () => null) });
    await f.actions.upload(() => f.design, {});
    expect(f.notices).toEqual([]); expect(f.design.assets).toEqual([]); expect(f.errors).toHaveLength(1);
  });
  it('reconciles a cancelled request that completed on the server without a false upload success', async () => {
    const upload = deferred(); const f = fixture({ uploadArtwork: vi.fn(() => upload.promise) });
    const action = f.actions.upload(() => f.design, {});
    await vi.waitFor(() => expect(f.api.uploadArtwork).toHaveBeenCalledOnce());
    f.actions.cancelUpload(); upload.resolve(asset); await action;
    expect(f.notices).toHaveLength(1); expect(f.notices[0]).toContain('Transfer stopped');
    expect(f.notices).not.toContain('Artwork uploaded securely.');
    expect(f.design.assets).toEqual([asset]); expect(f.errors).toEqual([]);
    expect(f.pending).toHaveBeenLastCalledWith(false);
  });
  it('stops before sending artwork when cancellation happens during automatic design save', async () => {
    const save = deferred(); const f = fixture();
    const action = f.actions.upload(() => save.promise, {});
    f.actions.cancelUpload(); save.resolve(f.design); await action;
    expect(f.api.uploadArtwork).not.toHaveBeenCalled(); expect(f.notices[0]).toContain('Transfer stopped');
  });
});
