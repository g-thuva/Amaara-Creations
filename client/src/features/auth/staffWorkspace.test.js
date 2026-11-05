import { describe, expect, it, vi } from 'vitest';
import { verifyStaffWorkspace } from './staffWorkspace.js';

describe('staff workspace availability', () => {
  it('checks a local development page without sending credentials', async () => {
    const fetchPage = vi.fn().mockResolvedValue({ type: 'opaque' });
    await verifyStaffWorkspace('http://localhost:5173/Amaara-Creations/login', { development: true, fetchPage });
    expect(fetchPage).toHaveBeenCalledWith('http://localhost:5173/Amaara-Creations/login', expect.objectContaining({ mode: 'no-cors', credentials: 'omit', cache: 'no-store', signal: expect.any(AbortSignal) }));
  });
  it('reports a stopped destination so the completed sign-in can be retried', async () => {
    const failure = new TypeError('Failed to fetch');
    await expect(verifyStaffWorkspace('http://127.0.0.1:5173/Amaara-Creations/login', { development: true, fetchPage: vi.fn().mockRejectedValue(failure) })).rejects.toBe(failure);
  });
  it('leaves deployed navigation independent of local page probes', async () => {
    const fetchPage = vi.fn();
    await verifyStaffWorkspace('https://management.example.test/login', { development: true, fetchPage });
    await verifyStaffWorkspace('http://localhost:5173/login', { development: false, fetchPage });
    expect(fetchPage).not.toHaveBeenCalled();
  });
});
