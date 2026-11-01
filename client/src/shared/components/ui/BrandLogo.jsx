const logoWhitePath = `${import.meta.env.BASE_URL}assets/brand/brand-logo-white.png`;
const logoBluePath = `${import.meta.env.BASE_URL}assets/brand/brand-logo-blue.png`;
const medallionWhiteCirclePath = `${import.meta.env.BASE_URL}assets/brand/brand-medallion-white-circle.png`;
const stackedWhitePath = `${import.meta.env.BASE_URL}assets/brand/brand-stacked-white.png`;
const stackedBluePath = `${import.meta.env.BASE_URL}assets/brand/brand-stacked-blue.png`;

/**
 * Official BrandLogo component for Amaara Creations.
 * Uses official vector assets in brand blue (#2d559a) or pure white (#ffffff)
 * depending on surface tone and placement.
 *
 * @param {'lockup' | 'medallion' | 'stacked' | 'wordmark'} [props.variant='lockup']
 * @param {'light' | 'dark' | 'blue'} [props.tone='light'] - 'light' for dark backgrounds (navbar/footer), 'dark'/'blue' for light backgrounds
 * @param {string} [props.className]
 */
export function BrandLogo({ variant = 'lockup', tone = 'light', className = '' }) {
  const isDarkTone = tone === 'dark' || tone === 'blue';

  if (variant === 'medallion') {
    return (
      <img
        src={medallionWhiteCirclePath}
        alt="Amaara Creations"
        className={`s-brand-medallion ${className}`}
        width="48"
        height="48"
        loading="eager"
        decoding="async"
      />
    );
  }

  if (variant === 'stacked') {
    const stackedSrc = isDarkTone ? stackedBluePath : stackedWhitePath;
    return (
      <img
        src={stackedSrc}
        alt="Amaara Creations"
        className={`s-brand-stacked ${className}`}
        width="160"
        height="138"
        loading="eager"
        decoding="async"
      />
    );
  }

  if (variant === 'wordmark') {
    const src = isDarkTone ? logoBluePath : logoWhitePath;
    return (
      <img
        src={src}
        alt="Amaara Creations"
        className={`s-brand-wordmark-img ${className}`}
        width="180"
        height="42"
        loading="eager"
        decoding="async"
      />
    );
  }

  // Default: horizontal lockup with brand mark & typography
  const src = isDarkTone ? logoBluePath : logoWhitePath;
  return (
    <div className={`s-brand-lockup ${className}`}>
      <img
        src={src}
        alt="Amaara Creations"
        className="s-brand-logo-img"
        width="188"
        height="44"
        loading="eager"
        decoding="async"
      />
    </div>
  );
}
