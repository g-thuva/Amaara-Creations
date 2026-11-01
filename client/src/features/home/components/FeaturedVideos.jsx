import { useEffect, useRef } from 'react';

const videoPath = `${import.meta.env.BASE_URL}assets/videos`;
const igAssetPath = `${import.meta.env.BASE_URL}assets/instagram`;

/*
 * Featured reels. Drop Instagram reels (downloaded as .mp4) into
 * client/public/assets/videos/ using the file names below. Until a video
 * file exists, the poster image is shown instead.
 */
const featuredVideos = [
  {
    id: 'v1',
    src: `${videoPath}/reel-1.mp4`,
    poster: `${igAssetPath}/mirror-arch-sunset.jpg`,
    title: 'Mirror arch at sunset',
    caption: 'Our arched mirror welcome sign coming to life.',
  },
  {
    id: 'v2',
    src: `${videoPath}/reel-2.mp4`,
    poster: `${igAssetPath}/tamil-wedding-sign.jpg`,
    title: 'Kalyanam ceremony sign',
    caption: 'Gold calligraphy for a traditional celebration.',
  },
  {
    id: 'v3',
    src: `${videoPath}/reel-3.mp4`,
    poster: `${igAssetPath}/she-said-yes-sign.jpg`,
    title: 'She said yes!',
    caption: 'A frosted acrylic proposal moment.',
  },
  {
    id: 'v4',
    src: `${videoPath}/reel-4.mp4`,
    poster: `${igAssetPath}/birthday-board.jpg`,
    title: 'Milestone birthday',
    caption: 'Hand-illustrated board for a 30th party.',
  },
];

function VideoCard({ video }) {
  const ref = useRef(null);

  // Play only while visible to save bandwidth and battery.
  useEffect(() => {
    const el = ref.current;
    if (!el || typeof IntersectionObserver === 'undefined') return undefined;
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) return undefined;
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) el.play().catch(() => {});
      else el.pause();
    }, { threshold: 0.4 });
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <figure className="s-video-card">
      <div className="s-video-frame">
        <video
          ref={ref}
          src={video.src}
          poster={video.poster}
          muted
          loop
          playsInline
          preload="none"
          aria-label={video.title}
        />
      </div>
      <figcaption>
        <strong>{video.title}</strong>
        <span>{video.caption}</span>
      </figcaption>
    </figure>
  );
}

export default function FeaturedVideos() {
  return (
    <div className="s-video-grid">
      {featuredVideos.map((video) => <VideoCard key={video.id} video={video} />)}
    </div>
  );
}

