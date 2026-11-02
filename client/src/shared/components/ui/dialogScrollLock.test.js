import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('dialog scroll locks', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.stubGlobal('document', { body: { style: { overflow: 'auto', paddingRight: '8px' } }, documentElement: { clientWidth: 1264, style: { scrollbarGutter: 'auto' } } });
    vi.stubGlobal('window', { innerWidth: 1280 });
    vi.stubGlobal('getComputedStyle', () => ({ scrollbarGutter: 'auto' }));
  });
  afterEach(() => vi.unstubAllGlobals());

  it('keeps the background locked when a nested confirmation closes first', async () => {
    const { lockDialogScroll } = await import('./dialogScrollLock.js');
    const outer = lockDialogScroll();
    const nested = lockDialogScroll();
    expect(document.body.style).toEqual({ overflow: 'hidden', paddingRight: '8px' });
    expect(document.documentElement.style.scrollbarGutter).toBe('stable');
    nested();
    expect(document.body.style.overflow).toBe('hidden');
    outer();
    expect(document.body.style).toEqual({ overflow: 'auto', paddingRight: '8px' });
    expect(document.documentElement.style.scrollbarGutter).toBe('auto');
  });

  it('restores original styles when routing unmounts outer and nested dialogs together', async () => {
    const { lockDialogScroll } = await import('./dialogScrollLock.js');
    const outer = lockDialogScroll();
    const nested = lockDialogScroll();
    outer();
    expect(document.body.style.overflow).toBe('hidden');
    outer();
    nested();
    expect(document.body.style).toEqual({ overflow: 'auto', paddingRight: '8px' });
    expect(document.documentElement.style.scrollbarGutter).toBe('auto');
    const next = lockDialogScroll();
    expect(document.body.style.overflow).toBe('hidden');
    next();
    expect(document.body.style.overflow).toBe('auto');
  });
  it('does not add a gutter to pages that have no scrollbar', async () => {
    document.documentElement.clientWidth = window.innerWidth;
    const { lockDialogScroll } = await import('./dialogScrollLock.js');
    const release = lockDialogScroll();
    expect(document.documentElement.style.scrollbarGutter).toBe('auto');
    expect(document.body.style.paddingRight).toBe('8px');
    release();
  });
  it('does not reserve a gutter that would overflow the minimum supported viewport', async () => {
    window.innerWidth = 320;
    document.documentElement.clientWidth = 305;
    vi.stubGlobal('getComputedStyle', () => ({ minWidth: '320px' }));
    const { lockDialogScroll } = await import('./dialogScrollLock.js');
    const release = lockDialogScroll();
    expect(document.documentElement.style.scrollbarGutter).toBe('auto');
    expect(document.body.style.overflow).toBe('hidden');
    release();
    expect(document.body.style.overflow).toBe('auto');
  });
});
