import { useCallback, useEffect, useId, useRef, useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';

const accepted = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'video/mp4', 'video/webm', 'video/quicktime']);

export function ProductMediaPicker({ files, onChange, disabled = false }) {
  const inputRef = useRef(null);
  const previews = useRef(new Set());
  const id = useId();
  const [dragging, setDragging] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => {
    const active = new Set(files.map(file => file.preview));
    for (const preview of previews.current) if (!active.has(preview)) { URL.revokeObjectURL(preview); previews.current.delete(preview); }
  }, [files]);
  useEffect(() => {
    const allocated = previews.current;
    return () => allocated.forEach(preview => URL.revokeObjectURL(preview));
  }, []);
  const addFiles = useCallback(incoming => {
    if (disabled) return;
    const selected = Array.from(incoming);
    const valid = selected.filter(file => accepted.has(file.type));
    setError(valid.length === selected.length ? '' : 'Choose a JPG, PNG, WebP, GIF, MP4, WebM or MOV file.');
    const added = valid.map(file => {
      const preview = URL.createObjectURL(file);
      previews.current.add(preview);
      return { file, altText: '', preview };
    });
    if (added.length) onChange(current => [...current, ...added]);
  }, [onChange, disabled]);

  return <div className="pf-media-section">
    <input id={id} ref={inputRef} type="file" tabIndex={-1} multiple accept={[...accepted].join(',')} className="pf-file-input"
      aria-label="Product images and videos" onChange={event => { addFiles(event.target.files); event.target.value = ''; }} />
    <button type="button" disabled={disabled} className={`pf-dropzone${dragging ? ' pf-dropzone--over' : ''}`}
      aria-describedby={`${id}-help`}
      onClick={() => inputRef.current?.click()}
      onDragOver={event => { event.preventDefault(); setDragging(true); }} onDragLeave={() => setDragging(false)}
      onDrop={event => { event.preventDefault(); setDragging(false); addFiles(event.dataTransfer.files); }}>
      <AppIcon name="image" />
      <span id={`${id}-label`} className="pf-dropzone-label"><strong>Drop photos and videos here</strong><span>or choose files from your device</span></span>
      <small id={`${id}-help`}>JPG, PNG, WebP, GIF, MP4, WebM and MOV. Upload limits are checked when saving.</small>
    </button>
    {error && <p className="admin-form-error" role="alert">{error}</p>}
    {files.length > 0 && <div className="pf-preview-grid">{files.map((item, index) => <div key={item.preview} className="pf-preview-card">
      {item.file.type.startsWith('video/') ? <video src={item.preview} className="pf-preview-thumb" muted playsInline controls aria-label={item.file.name} /> : <img src={item.preview} alt={item.altText || item.file.name} className="pf-preview-thumb" />}
      <span className="pf-file-name">{item.file.name}</span>
      <label className="form-group">Caption / alt text {index + 1}<input className="form-control" value={item.altText}
        onChange={event => onChange(current => current.map(file => file === item ? { ...file, altText: event.target.value } : file))} /></label>
      <div className="pf-media-actions">
        <button type="button" className="btn btn-outline-primary" disabled={index === 0} aria-label={`Move ${item.file.name} earlier`}
          onClick={() => onChange(current => { const next = [...current]; [next[index - 1], next[index]] = [next[index], next[index - 1]]; return next; })}>Move up</button>
        <button type="button" className="btn btn-outline-primary" aria-label={`Remove ${item.file.name}`} onClick={() => onChange(current => current.filter(file => file !== item))}>Remove</button>
      </div>
    </div>)}</div>}
  </div>;
}
