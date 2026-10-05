import { Link } from 'react-router-dom';
import { safeLink } from '../../utils/storefront';
import { Media } from './UI';

// Phase 3 stores text or structured JSON. Unknown blocks are deliberately omitted.
export default function CmsSections({ sections = [] }) {
  return <div className="s-cms-sections">{sections.filter(s => s.isActive).map(section => {
    if (section.contentType === 'text') return <section key={section.id} className="s-prose"><p>{section.content}</p></section>;
    if (section.contentType !== 'json') return null;
    let block;
    try { block = JSON.parse(section.content); } catch { return null; }
    if (!block || typeof block !== 'object') return null;
    const title = typeof block.title === 'string' ? block.title : null;
    const body = typeof block.body === 'string' ? block.body : null;
    if (block.type === 'faq' && Array.isArray(block.items)) return <section key={section.id} className="s-faq">{title && <h2>{title}</h2>}{block.items.filter(i => i && typeof i.question === 'string' && typeof i.answer === 'string').map((item, index) => <details key={index}><summary>{item.question}</summary><p>{item.answer}</p></details>)}</section>;
    if (['text', 'hero', 'cta', 'image'].includes(block.type)) {
      const href = safeLink(block.ctaUrl);
      return <section key={section.id} className={`s-prose s-cms-${block.type}`}>
        {title && <h2>{title}</h2>}{body && <p>{body}</p>}
        {typeof block.imageUrl === 'string' && <Media src={block.imageUrl} alt={typeof block.alt === 'string' ? block.alt : title || ''}/>}
        {href && typeof block.ctaLabel === 'string' && (href.startsWith('/') ? <Link className="s-button s-button-secondary" to={href}>{block.ctaLabel}</Link> : <a className="s-button s-button-secondary" href={href} rel="noopener noreferrer">{block.ctaLabel}</a>)}
      </section>;
    }
    return null;
  })}</div>;
}
