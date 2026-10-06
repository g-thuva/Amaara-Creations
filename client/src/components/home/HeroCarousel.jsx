import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Icon, Media } from '../storefront/UI';
import { normaliseHeroSlides } from './heroSlides';

function CarouselLink({ href, className, children }) {
  if (!href) return null;
  if (href.startsWith('/')) return <Link className={className} to={href}>{children}</Link>;
  return <a className={className} href={href} target="_blank" rel="noopener noreferrer">{children}</a>;
}

export default function HeroCarousel({ hero }) {
  const slides = useMemo(() => normaliseHeroSlides(hero), [hero]);
  const [current, setCurrent] = useState(0);
  const [paused, setPaused] = useState(false);
  const [interacting, setInteracting] = useState(false);
  const [reducedMotion, setReducedMotion] = useState(false);
  const touchStart = useRef(null);
  const regionRef = useRef(null);
  const multiple = slides.length > 1;

  useEffect(() => {
    const query = window.matchMedia('(prefers-reduced-motion: reduce)');
    const update = () => {
      setReducedMotion(query.matches);
      if (query.matches) setPaused(true);
    };
    update();
    query.addEventListener?.('change', update);
    return () => query.removeEventListener?.('change', update);
  }, []);

  useEffect(() => {
    if (!multiple || paused || interacting || reducedMotion) return undefined;
    const timer = window.setInterval(() => setCurrent((index) => (index + 1) % slides.length), 5000);
    return () => window.clearInterval(timer);
  }, [interacting, multiple, paused, reducedMotion, slides.length]);

  const move = (direction) => setCurrent((index) => (index + direction + slides.length) % slides.length);

  return (
    <section className="s-hero-shell">
      <div
        ref={regionRef}
        className="s-hero-carousel"
        role="region"
        aria-roledescription="carousel"
        aria-label="Featured"
        onMouseEnter={() => setInteracting(true)}
        onMouseLeave={() => setInteracting(false)}
        onFocusCapture={() => setInteracting(true)}
        onBlurCapture={(event) => {
          if (!event.currentTarget.contains(event.relatedTarget)) setInteracting(false);
        }}
        onPointerDown={(event) => {
          if (event.pointerType === 'touch') {
            touchStart.current = event.clientX;
            setInteracting(true);
          }
        }}
        onPointerUp={(event) => {
          if (event.pointerType !== 'touch' || touchStart.current === null) return;
          const distance = event.clientX - touchStart.current;
          if (Math.abs(distance) > 45) move(distance > 0 ? -1 : 1);
          touchStart.current = null;
          setInteracting(false);
        }}
      >
        {slides.map((slide, index) => (
          <article
            key={`${slide.title}-${index}`}
            className={`s-hero-slide s-hero-align-${slide.align} ${index === current ? 'is-active' : ''}`}
            role="group"
            aria-roledescription="slide"
            aria-label={`${index + 1} of ${slides.length}`}
            aria-hidden={index !== current}
            inert={index !== current}
          >
            {slide.imageUrl ? (
              <Media
                className="s-hero-media"
                src={slide.imageUrl}
                alt={slide.alt || slide.title}
                eager={index === 0}
                width={1600}
                height={900}
                objectPosition={slide.objectPosition}
              />
            ) : (
              <div className="s-hero-fallback" aria-hidden="true">
                <span /><span /><span /><span /><span />
              </div>
            )}
            <div className="s-hero-overlay" aria-hidden="true" />
            <div className="s-hero-content">
              <p className="s-hero-kicker">Amaara Creations</p>
              <h1>{slide.title}</h1>
              {slide.body && <p>{slide.body}</p>}
              {slide.ctaUrl && slide.ctaLabel && (
                <CarouselLink className="s-button s-button-light" href={slide.ctaUrl}>
                  {slide.ctaLabel}<Icon name="arrow" />
                </CarouselLink>
              )}
            </div>
          </article>
        ))}

        {multiple && (
          <>
            <Button className="s-carousel-arrow s-carousel-prev" variant="glass" aria-label="Previous slide" onClick={() => move(-1)}>
              <Icon name="chevronLeft" />
            </Button>
            <Button className="s-carousel-arrow s-carousel-next" variant="glass" aria-label="Next slide" onClick={() => move(1)}>
              <Icon name="chevronRight" />
            </Button>
            <div className="s-carousel-controls">
              <Button variant="glass" aria-label={paused ? 'Play carousel' : 'Pause carousel'} aria-pressed={paused} onClick={() => setPaused((value) => !value)}>
                <Icon name={paused ? 'play' : 'pause'} />
              </Button>
              <div className="s-carousel-dots" aria-label="Choose a slide">
                {slides.map((slide, index) => (
                  <button
                    key={`${slide.title}-dot-${index}`}
                    type="button"
                    aria-label={`Go to slide ${index + 1} of ${slides.length}`}
                    aria-current={index === current ? 'true' : undefined}
                    onClick={() => setCurrent(index)}
                  />
                ))}
              </div>
            </div>
          </>
        )}
      </div>
    </section>
  );
}
