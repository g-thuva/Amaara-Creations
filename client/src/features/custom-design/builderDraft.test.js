import { expect, it } from 'vitest';
import { readBuilderDraft, saveBuilderDraft, clearBuilderDraft } from './builderDraft';

it.each(['{broken', 'null', '[]', '42', '"text"'])('ignores an invalid browser draft %s', value => {
  expect(readBuilderDraft({ getItem: () => value })).toBeNull();
});
it('retains an editable draft and clears it only after persistence', () => {
  const values = new Map();
  const storage = { getItem: key => values.get(key), setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) };
  saveBuilderDraft({ name: 'Labels', width: '4' }, storage);
  expect(readBuilderDraft(storage)).toEqual({ name: 'Labels', width: '4' });
  clearBuilderDraft(storage);
  expect(readBuilderDraft(storage)).toBeNull();
});
it('keeps the builder usable when browser storage is denied', () => {
  const denied = () => { throw new Error('Storage denied'); };
  const storage = { getItem: denied, setItem: denied, removeItem: denied };
  expect(readBuilderDraft(storage)).toBeNull();
  expect(() => saveBuilderDraft({}, storage)).not.toThrow();
  expect(() => clearBuilderDraft(storage)).not.toThrow();
});
