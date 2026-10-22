import { useEffect, useRef, useState } from 'react';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';

export default function AvatarCropDialog({ file, onClose, onSave, pending }) {
  const [url, setUrl] = useState('');
  const [size, setSize] = useState(null);
  const [zoom, setZoom] = useState(1);
  const [x, setX] = useState(50);
  const [y, setY] = useState(50);
  const [error, setError] = useState('');
  const imageRef = useRef(null);
  const saving = useRef(false);
  useEffect(() => {
    const objectUrl = URL.createObjectURL(file); setUrl(objectUrl);
    return () => URL.revokeObjectURL(objectUrl);
  }, [file]);
  const scale = size ? Math.max(256 / size.width, 256 / size.height) * zoom : 1;
  const width = size ? size.width * scale : 256;
  const height = size ? size.height * scale : 256;
  async function save() {
    if (!size || pending || saving.current) return;
    saving.current = true;
    setError('');
    try {
      const canvas = document.createElement('canvas'); canvas.width = 512; canvas.height = 512;
      canvas.getContext('2d').drawImage(imageRef.current, (256 - width) * x / 100 * 2, (256 - height) * y / 100 * 2, width * 2, height * 2);
      const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
      if (!blob) throw new Error('Unable to prepare the photo. Choose another image.');
      const saved = await onSave(new File([blob], 'profile-photo.png', { type: 'image/png' }));
      if (saved) onClose(); else setError('The photo was not saved. Your previous photo remains in place. Try again.');
    } catch (error) { setError(error.message); }
    finally { saving.current = false; }
  }
  return <Dialog open onClose={pending ? () => {} : onClose} title="Crop profile photo"><p>Adjust the square crop before saving. Animated images use a still frame.</p>{error && <p role="alert" className="s-error">{error}</p>}<div className="s-avatar-crop">{url && <img ref={imageRef} src={url} alt="Profile photo crop preview" onError={() => setError('This image could not be opened. Choose a complete image file.')} onLoad={event => { const image = event.currentTarget; if (image.naturalWidth * image.naturalHeight > 24_000_000) setError('This image is too large. Choose a smaller photo.'); else setSize({ width: image.naturalWidth, height: image.naturalHeight }); }} style={{ width, height, maxWidth: 'none', left: (256 - width) * x / 100, top: (256 - height) * y / 100 }} />}</div><div className="s-form"><label>Photo zoom<input type="range" min="1" max="4" step=".05" value={zoom} onChange={event => setZoom(Number(event.target.value))} disabled={!size || pending} /></label><label>Horizontal crop position<input type="range" min="0" max="100" value={x} onChange={event => setX(Number(event.target.value))} disabled={!size || pending} /></label><label>Vertical crop position<input type="range" min="0" max="100" value={y} onChange={event => setY(Number(event.target.value))} disabled={!size || pending} /></label></div><div className="s-actions"><Button type="button" variant="secondary" disabled={pending} onClick={onClose}>Cancel</Button><Button type="button" disabled={!size || pending} onClick={save}>{pending ? 'Saving photo…' : 'Save cropped photo'}</Button></div></Dialog>;
}
