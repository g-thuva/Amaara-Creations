export function previewGeometry(shapeCode, width, height) {
  const w = Number(width);
  const h = Number(height);
  const ratio = Number.isFinite(w) && Number.isFinite(h) && w > 0 && h > 0 ? w / h : 1;
  const code = String(shapeCode || '').toLowerCase().replace(/[ _-]/g, '');
  const shape = ['circle', 'round'].includes(code) ? 'circle'
    : ['oval', 'ellipse'].includes(code) ? 'oval'
    : ['rounded', 'roundedrectangle', 'roundrect'].includes(code) ? 'rounded'
    : ['rectangle', 'rect', 'square'].includes(code) ? 'rectangle' : 'contour';
  return { shape, ratio, width: ratio >= 1 ? 100 : ratio * 100, height: ratio >= 1 ? 100 / ratio : 100,
    exact: shape !== 'contour' && (shape !== 'circle' || Math.abs(ratio - 1) < 0.001) };
}

export function changePreviewSpecification(form, field, value) {
  const next = { ...form, [field]: value };
  if (previewGeometry(next.shapeCode, next.width, next.height).shape === 'circle') {
    if (field === 'shapeCode' || field === 'width') next.height = next.width;
    if (field === 'height') next.width = next.height;
  }
  return next;
}
