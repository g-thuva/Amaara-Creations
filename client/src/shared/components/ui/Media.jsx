import { useState } from 'react';
import Icon from '@/shared/components/ui/Icon.jsx';
import { resolveMediaUrl } from '@/shared/api/requestConfig.js';
export function Media({
  src,
  alt,
  eager = false,
  className = "",
  width = 800,
  height = 600,
  objectPosition,
}) {
  const [failed, setFailed] = useState(null);
  const url = resolveMediaUrl(src);

  return (
    <div className={`s-media ${className}`.trim()}>
      {url && failed !== url ? (
        <img
          src={url}
          alt={alt || ""}
          width={width}
          height={height}
          loading={eager ? "eager" : "lazy"}
          fetchPriority={eager ? "high" : "auto"}
          decoding="async"
          style={objectPosition ? { objectPosition } : undefined}
          onError={() => setFailed(url)}
        />
      ) : (
        <div
          className="s-image-fallback"
          role="img"
          aria-label={`${alt || "Product"} â€” image unavailable`}
        >
          <span className="s-sticker-glyph" aria-hidden="true">
            <Icon name="image" />
          </span>
          <span>Image coming soon</span>
        </div>
      )}
    </div>
  );
}
