import { useEffect, useState } from 'react';
import { cartApi } from '@/features/cart/cartApi.js';
import { Media } from '@/shared/components/ui/Media.jsx';

export default function CartArtwork({ item }) {
  const [source, setSource] = useState(null);
  useEffect(() => {
    if (!item.artworkDownloadUrl) return undefined;
    const controller = new AbortController(); let active = true; let url;
    cartApi.downloadArtwork(item.artworkDownloadUrl, controller.signal).then(blob => {
      if (!active) return;
      url = URL.createObjectURL(blob); setSource({ reference: item.artworkDownloadUrl, url });
    }).catch(() => { if (active) setSource(null); });
    return () => { active = false; controller.abort(); if (url) URL.revokeObjectURL(url); };
  }, [item.artworkDownloadUrl]);
  return <Media src={source && source.reference === item.artworkDownloadUrl ? source.url : item.productImageUrl} alt={item.productName} width={300} height={225} />;
}
