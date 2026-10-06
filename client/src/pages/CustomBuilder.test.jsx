import { describe, expect, it } from 'vitest';
import { customDesignRequest, optionsByGroup, readCustomSnapshot } from '../utils/customBuilder';

describe('Custom Builder custom builder presentation adapters', () => {
  it('groups only active server configuration options', () => {
    const grouped = optionsByGroup([
      { group: 'Shape', code: 'round', isActive: true },
      { group: 'Shape', code: 'retired', isActive: false },
      { group: 'Material', code: 'standard', isActive: true },
    ]);
    expect(grouped.Shape.map((option) => option.code)).toEqual(['round']);
    expect(grouped.Material.map((option) => option.code)).toEqual(['standard']);
  });

  it('normalises numeric form values without accepting a browser price', () => {
    const request = customDesignRequest({ name: 'Labels', width: '10', height: '5', quantity: '25', customText: 'Amaara', textAlignment: 'center' });
    expect(request).toMatchObject({ width: 10, height: 5, quantity: 25 });
    expect(request).not.toHaveProperty('price');
    expect(JSON.parse(request.editorStateJson).schemaVersion).toBe(1);
  });

  it('accepts versioned order snapshots and rejects malformed or unknown versions', () => {
    expect(readCustomSnapshot('{"SnapshotVersion":1,"DesignName":"Labels"}')).toMatchObject({ DesignName: 'Labels' });
    expect(readCustomSnapshot('{"SnapshotVersion":2}')).toBeNull();
    expect(readCustomSnapshot('not json')).toBeNull();
  });
});
