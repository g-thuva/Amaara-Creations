import { describe, expect, it } from 'vitest';
import { changePreviewSpecification, previewGeometry } from './previewGeometry.js';

describe('dimensional sticker preview', () => {
  it('keeps circular dimensions equal while leaving rectangles independently editable', () => {
    const round = changePreviewSpecification({ shapeCode: 'rectangle', width: 8, height: 4 }, 'shapeCode', 'round');
    expect(round).toMatchObject({ width: 8, height: 8 });
    expect(changePreviewSpecification(round, 'height', 5)).toMatchObject({ width: 5, height: 5 });
    expect(changePreviewSpecification({ shapeCode: 'rectangle', width: 8, height: 4 }, 'height', 5)).toMatchObject({ width: 8, height: 5 });
  });
  it.each([['rectangle', 8, 4, 'rectangle', 2], ['rounded-rectangle', 3, 6, 'rounded', .5], ['circle', 4, 4, 'circle', 1], ['oval', 6, 4, 'oval', 1.5]])('preserves %s geometry', (code, width, height, shape, ratio) => {
    const result = previewGeometry(code, width, height);
    expect(result.shape).toBe(shape);
    expect(result.ratio).toBe(ratio);
    expect(result.width / result.height).toBeCloseTo(ratio);
    expect(Math.max(result.width, result.height)).toBe(100);
  });
  it('does not invent a die-cut contour or a false circle', () => {
    expect(previewGeometry('die-cut', 3, 5).exact).toBe(false);
    expect(previewGeometry('circle', 4, 5).exact).toBe(false);
  });
  it.each([[0, 1], ['invalid', 5], [-1, 4], [1, Infinity]])('bounds invalid dimensions %s / %s', (width, height) => {
    const result = previewGeometry('rectangle', width, height);
    expect(Number.isFinite(result.ratio)).toBe(true);
    expect(result.width).toBeGreaterThan(0);
    expect(result.height).toBeGreaterThan(0);
  });
});
