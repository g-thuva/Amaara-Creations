import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';

const videoPath = `${import.meta.env.BASE_URL}assets/videos`;
const igAssetPath = `${import.meta.env.BASE_URL}assets/instagram`;

/*
 * Featured reels. Drop Instagram reels (downloaded as .mp4) into
 * client/public/assets/videos/ using the file names below. Until a video
 * file exists, the poster image is shown instead with seamless fallback.
 */
const featuredVideos = [
  {
    id: 'v1',
    src: `${videoPath}/reel-1.mp4`,
    poster: `${igAssetPath}/mirror-arch-sunset.jpg`,
    title: 'Mirror arch at sunset',
    caption: 'Our arched mirror welcome sign coming to life.',
    to: '/products?search=mirror',
  },
  {
    id: 'v2',
    src: `${videoPath}/reel-2.mp4`,
    poster: `${igAssetPath}/tamil-wedding-sign.jpg`,
    title: 'Kalyanam ceremony sign',
    caption: 'Gold calligraphy for a traditional celebration.',
    to: '/products?search=kalyanam',
  },
  {
    id: 'v3',
    src: `${videoPath}/reel-3.mp4`,
    poster: `${igAssetPath}/she-said-yes-sign.jpg`,
    title: 'She said yes!',
    caption: 'A frosted acrylic proposal moment.',
    to: '/products?search=acrylic',
  },
  {
    id: 'v4',
    src: `${videoPath}/reel-4.mp4`,
    poster: `${igAssetPath}/birthday-board.jpg`,
    title: 'Milestone birthday',
    caption: 'Hand-illustrated board for a 30th party.',
    to: '/products?search=birthday',
  },
];

function VideoCard({ video }) {
  const ref = useRef(null);
  const [videoLoaded, setVideoLoaded] = useState(false);
  const [videoError, setVideoError] = useState(false);

  // Play only while visible to save bandwidth and battery.
  useEffect(() => {
    const el = ref.current;
    if (!el || videoError || typeof IntersectionObserver === 'undefined') return undefined;
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) return undefined;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !videoError) {
          el.play().catch(() => {});
        } else {
          el.pause();
        }
      },
      { threshold: 0.35 },
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [videoError]);

  return (
    <figure className="s-video-card">
      <Link
        to={video.to}
        className="s-video-link"
      >
        <div className="s-video-frame">
          <span className="s-video-overlay" aria-hidden="true" />

          {video.src && !videoError && (
            <video
              ref={ref}
              src={video.src}
              poster={video.poster}
              muted
              loop
              playsInline
              preload="none"
              aria-label={video.title}
              onLoadedData={() => setVideoLoaded(true)}
              onError={() => setVideoError(true)}
            />
          )}

          <img
            src={video.poster}
            alt={video.title}
            loading="lazy"
            decoding="async"
            className={`s-video-poster ${videoLoaded && !videoError ? 'is-hidden' : ''}`}
          />
        </div>

        <figcaption className="s-video-caption">
          <strong className="s-video-title">{video.title}</strong>
          <span className="s-video-text">{video.caption}</span>
        </figcaption>
      </Link>
    </figure>
  );
}

export default function FeaturedVideos() {
  return (
    <div className="s-video-grid">
      {featuredVideos.map((video) => (
        <VideoCard key={video.id} video={video} />
      ))}
    </div>
  );
}

