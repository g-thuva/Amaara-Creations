import { expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import CmsSections from './CmsSections';
  it('escapes CMS text and suppresses unknown blocks and unsafe CTAs', () => {
    const markup = renderToStaticMarkup(<MemoryRouter><CmsSections sections={[
      { id: 1, isActive: true, contentType: 'text', content: '<script>alert(1)</script>' },
      { id: 2, isActive: true, contentType: 'json', content: JSON.stringify({ type: 'cta', title: 'Hello', ctaUrl: 'javascript:alert(1)', ctaLabel: 'Bad link' }) },
      { id: 3, isActive: true, contentType: 'json', content: '{broken' },
    ]}/></MemoryRouter>);
    expect(markup).toContain('&lt;script&gt;'); expect(markup).not.toContain('<script>'); expect(markup).not.toContain('href='); expect(markup).toContain('Hello');
  });
