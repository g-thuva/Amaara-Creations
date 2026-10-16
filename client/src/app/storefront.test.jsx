import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import ProductCard from '@/features/catalog/components/ProductCard.jsx';
import { Quantity } from '@/shared/components/ui/Quantity.jsx';
import ProtectedRoute from '@/app/guards/AuthGuard.jsx';
import HeroCarousel from '@/features/home/components/HeroCarousel.jsx';
import CategoryCards from '@/features/home/components/CategoryCards.jsx';
import Footer from '@/app/layout/Footer.jsx';
import { normaliseHeroSlides } from '@/features/home/components/heroSlides.js';
vi.mock('@/features/content/SiteContentProvider.jsx', () => ({ useSiteContent: () => ({ values: {} }) }));
vi.mock('@/features/wishlist/WishlistProvider.jsx', () => ({ useWishlist: () => ({ wishlist: { data: { items: [{ productId: 7 }] } }, pending: false, toggleWishlist: vi.fn() }) }));
vi.mock('@/features/auth/AuthProvider.jsx', () => ({ useAuth: () => ({ authStatus: 'loading', isAuthenticated: false }) }));
describe('storefront presentation', () => {
  it('renders actual product price, name, availability and wishlist state', () => {
    const markup = renderToStaticMarkup(<MemoryRouter><ProductCard product={{ id: 7, name: 'Printed label', category: 'Labels', price: 125, stock: 0, imageUrl: '' }}/></MemoryRouter>);
    expect(markup).toContain('Printed label'); expect(markup).toContain('125.00'); expect(markup).toContain('Out of stock'); expect(markup).toContain('aria-pressed="true"'); expect(markup).toContain('/products/7');
  });
  it('labels explicitly priced CHF products without an LKR symbol', () => {
    const markup = renderToStaticMarkup(<MemoryRouter><ProductCard product={{ id: 8, name: 'TEST CHF label', currency: 'CHF', price: 25.5, stock: 1, imageUrl: '' }}/></MemoryRouter>);
    expect(markup).toContain('CHF'); expect(markup).toContain('25.50'); expect(markup).not.toContain('Rs.');
  });
  it('disables increase at known stock and exposes labelled quantity', () => {
    const markup = renderToStaticMarkup(<Quantity value={3} max={3} onChange={() => {}}/>);
    expect(markup).toContain('max="3"'); expect(markup).toMatch(/disabled="" aria-label="Increase quantity"/);
  });
  it('does not show protected content during restoration', () => {
    const markup = renderToStaticMarkup(<MemoryRouter><ProtectedRoute><p>Private details</p></ProtectedRoute></MemoryRouter>);
    expect(markup).toContain('Restoring session'); expect(markup).not.toContain('Private details');
  });
  it('normalises single and multi-slide CMS hero contracts and limits the carousel', () => {
    expect(normaliseHeroSlides({ title: 'Wedding stickers', ctaUrl: '/products' })[0].title).toBe('Wedding stickers');
    const slides = normaliseHeroSlides({ slides: Array.from({ length: 7 }, (_, index) => ({ title: `Slide ${index + 1}` })) });
    expect(slides).toHaveLength(5);
    expect(normaliseHeroSlides({ title: 'Safe', ctaUrl: 'javascript:alert(1)' })[0].ctaUrl).toBeNull();
    expect(normaliseHeroSlides(null)[0].title).toBe('Custom stickers & decals');
  });
  it('renders labelled carousel controls only for multiple slides', () => {
    const single = renderToStaticMarkup(<MemoryRouter><HeroCarousel hero={{ title: 'One slide' }} /></MemoryRouter>);
    const multiple = renderToStaticMarkup(<MemoryRouter><HeroCarousel hero={{ slides: [{ title: 'One' }, { title: 'Two' }] }} /></MemoryRouter>);
    expect(single).toContain('aria-roledescription="carousel"');
    expect(single).not.toContain('Pause carousel');
    expect(multiple).toContain('Pause carousel');
    expect(multiple).toContain('Go to slide 2 of 2');
  });
  it('handles zero, one and many category cards', () => {
    expect(renderToStaticMarkup(<MemoryRouter><CategoryCards categories={[]} /></MemoryRouter>)).toBe('');
    const one = renderToStaticMarkup(<MemoryRouter><CategoryCards categories={[{ id: 1, name: 'Wedding', imageUrl: null }]} /></MemoryRouter>);
    const many = renderToStaticMarkup(<MemoryRouter><CategoryCards categories={[{ id: 1, name: 'Wedding' }, { id: 2, name: 'Car decals' }]} /></MemoryRouter>);
    expect(one).toContain('Wedding</h3>');
    expect(one).toContain('Shop now');
    expect(many).toContain('Car decals');
  });
  it('hides contact and social footer columns when settings are empty', () => {
    const markup = renderToStaticMarkup(<MemoryRouter><Footer /></MemoryRouter>);
    expect(markup).toContain('Quick links');
    expect(markup).not.toContain('<h2>Contact</h2>');
    expect(markup).not.toContain('Stay in touch');
  });
});
