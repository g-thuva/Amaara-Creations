import { Media } from '@/shared/components/ui/Media.jsx';
export default function ProductGallery({ product, gallery, image, setImage, moveImage }) {
  return (
        <section
          className="s-pdp-gallery"
          aria-label="Product gallery"
          tabIndex="0"
          onKeyDown={(event) => {
            if (event.key === 'ArrowLeft') moveImage(-1);
            if (event.key === 'ArrowRight') moveImage(1);
          }}
        >
          <Media eager src={gallery[image]?.url} alt={gallery[image]?.altText || product.name} width={1000} height={1000} />
          {gallery.length > 1 && (
            <div className="s-thumbnails" aria-label="Product images">
              {gallery.map((media, index) => (
                <button key={media.id || index} type="button" aria-label={`View image ${index + 1}`} aria-pressed={index === image} onClick={() => setImage(index)}>
                  <Media src={media.url} alt={media.altText || `${product.name}, image ${index + 1}`} width={160} height={160} />
                </button>
              ))}
            </div>
          )}
          {gallery.length > 1 && <p className="s-gallery-hint">Use the thumbnails or keyboard arrow keys to browse images.</p>}
        </section>
  );
}
