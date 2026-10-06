import { Link } from 'react-router-dom';
import { safeLink } from '../../utils/storefront';
import { Media } from './UI';

function ActionLink({ href, children }) {
  if (!href) return null;
  if (href.startsWith('/')) return <Link className="s-button s-button-secondary" to={href}>{children}</Link>;
  return <a className="s-button s-button-secondary" href={href} target="_blank" rel="noopener noreferrer">{children}</a>;
}

function JsonBlock({ section, block }) {
  const title = typeof block.title === 'string' ? block.title : null;
  const body = typeof block.body === 'string' ? block.body : null;

  if (block.type === 'faq' && Array.isArray(block.items)) {
    const items = block.items.filter((item) => item && typeof item.question === 'string' && typeof item.answer === 'string');
    if (!items.length) return null;
    return (
      <section className="s-faq" key={section.id}>
        {title && <h2>{title}</h2>}
        {items.map((item, index) => (
          <details key={`${item.question}-${index}`}>
            <summary>{item.question}</summary>
            <p>{item.answer}</p>
          </details>
        ))}
      </section>
    );
  }

  if (block.type === 'gallery' && Array.isArray(block.items)) {
    const images = block.items.filter((item) => item && typeof item.imageUrl === 'string');
    if (!images.length) return null;
    return (
      <section className="s-cms-gallery" key={section.id}>
        {title && <h2>{title}</h2>}
        {body && <p>{body}</p>}
        <div>
          {images.map((item, index) => (
            <figure key={`${item.imageUrl}-${index}`}>
              <Media src={item.imageUrl} alt={typeof item.alt === 'string' ? item.alt : title || 'Amaara work'} width={800} height={800} />
              {typeof item.caption === 'string' && <figcaption>{item.caption}</figcaption>}
            </figure>
          ))}
        </div>
      </section>
    );
  }

  if (!['text', 'hero', 'cta', 'image'].includes(block.type)) return null;
  const href = safeLink(block.ctaUrl);

  return (
    <section key={section.id} className={`s-prose s-cms-${block.type}`}>
      {title && <h2>{title}</h2>}
      {body && <p>{body}</p>}
      {typeof block.imageUrl === 'string' && (
        <figure>
          <Media src={block.imageUrl} alt={typeof block.alt === 'string' ? block.alt : title || 'Amaara Creations'} width={1200} height={750} />
          {typeof block.caption === 'string' && <figcaption>{block.caption}</figcaption>}
        </figure>
      )}
      {href && typeof block.ctaLabel === 'string' && <ActionLink href={href}>{block.ctaLabel}</ActionLink>}
    </section>
  );
}

// Catalogue Media stores text or structured JSON. Unknown blocks are deliberately omitted.
export default function CmsSections({ sections = [] }) {
  return (
    <div className="s-cms-sections">
      {sections.filter((section) => section.isActive).map((section) => {
        if (section.contentType === 'text') {
          return <section key={section.id} className="s-prose"><p>{section.content}</p></section>;
        }
        if (section.contentType !== 'json') return null;
        try {
          const block = JSON.parse(section.content);
          return block && typeof block === 'object' ? <JsonBlock key={section.id} section={section} block={block} /> : null;
        } catch {
          return null;
        }
      })}
    </div>
  );
}
