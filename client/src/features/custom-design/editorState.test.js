import { expect, it } from 'vitest';
import { artworkPlacement, defaultEditor, readEditorState } from './editorState.js';

it('fits or fills a differently shaped image without distorting pixels', () => {
  expect(artworkPlacement(100, 50, 200, 200, defaultEditor)).toEqual({ x: 0, y: 50, width: 200, height: 100 });
  expect(artworkPlacement(100, 50, 200, 200, { ...defaultEditor, artworkFit: 'cover' })).toEqual({ x: -100, y: 0, width: 400, height: 200 });
  expect(artworkPlacement(100, 50, 200, 200, { ...defaultEditor, artworkFit: 'cover', artworkX: 100 })).toEqual({ x: -200, y: 0, width: 400, height: 200 });
});
it('recovers safely from old, malformed, unbounded and active editor state', () => {
  expect(readEditorState('{broken')).toEqual(defaultEditor);
  expect(readEditorState({ schemaVersion: 1, textSize: 99 })).toEqual(defaultEditor);
  const read = readEditorState({ schemaVersion: 2, artworkScale: 999, textY: -100, artworkRotation: Infinity, background: 'url(javascript:1)', textSize: 0 });
  expect(read).toMatchObject({ artworkScale: 4, textY: 0, artworkRotation: 0, background: 'white', textSize: 2 });
});
