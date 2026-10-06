import { safeLink } from '../../utils/storefront';

const fallbackSlide = {
  title: 'Custom stickers & decals',
  body: 'Create personal details for weddings, cars, gifts and everyday things.',
  ctaLabel: 'Shop stickers',
  ctaUrl: '/products',
  imageUrl: '',
  alt: '',
  align: 'left',
};

export function normaliseHeroSlides(hero) {
  const candidates = Array.isArray(hero?.slides) ? hero.slides.slice(0, 5) : hero && typeof hero === 'object' ? [hero] : [];
  const slides = candidates
    .filter((slide) => slide && typeof slide === 'object')
    .map((slide, index) => ({
      title: typeof slide.title === 'string' && slide.title.trim() ? slide.title.trim() : index === 0 ? fallbackSlide.title : '',
      body: typeof slide.body === 'string' ? slide.body.trim() : '',
      ctaLabel: typeof slide.ctaLabel === 'string' ? slide.ctaLabel.trim() : '',
      ctaUrl: safeLink(slide.ctaUrl),
      imageUrl: typeof slide.imageUrl === 'string' ? slide.imageUrl : '',
      alt: typeof slide.alt === 'string' ? slide.alt : '',
      align: ['left', 'center', 'right'].includes(slide.align) ? slide.align : 'left',
      objectPosition: typeof slide.objectPosition === 'string' ? slide.objectPosition : undefined,
    }))
    .filter((slide) => slide.title || slide.body || slide.imageUrl);

  return slides.length ? slides : [fallbackSlide];
}
