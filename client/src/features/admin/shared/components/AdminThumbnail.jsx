import { useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';

export function AdminThumbnail({ src, alt = 'Media asset' }) {
  const [failed, setFailed] = useState(null);
  if (!src || failed === src) return <span className="product-thumbnail admin-thumbnail-fallback" role="img" aria-label={alt + ' — image unavailable'} title="Image unavailable"><AppIcon name="image" /></span>;
  return <img className="product-thumbnail" src={src} alt={alt} onError={() => setFailed(src)} />;
}
