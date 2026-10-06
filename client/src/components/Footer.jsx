import { Link } from 'react-router-dom';
import { useStore } from '../contexts/StoreContext';
import { Icon } from './storefront/UI';
import { safeLink } from '../utils/storefront';

function SocialLink({ name, href }) {
  const url = safeLink(href);
  if (!url?.startsWith('https://')) return null;
  return (
    <a href={url} target="_blank" rel="noopener noreferrer">
      <Icon name={name === 'instagram' ? 'instagram' : name === 'facebook' ? 'facebook' : 'arrow'} />
      <span>{name}</span>
    </a>
  );
}

export default function Footer() {
  const { values } = useStore();
  const siteName = values['site.name'] || 'Amaara Creations';
  const email = values['contact.email'];
  const phone = values['contact.phone'];
  const address = values['contact.address'];
  const whatsapp = values['contact.whatsapp'] || values['social.whatsapp'];
  const socials = ['instagram', 'facebook', 'tiktok']
    .map((name) => [name, values[`social.${name}`]])
    .filter(([, href]) => safeLink(href)?.startsWith('https://'));
  const hasContact = email || phone || address || whatsapp;

  return (
    <footer className="s-footer">
      <div className="s-footer-pattern" aria-hidden="true" />
      <div className="s-container">
        <div className="s-footer-top">
          <section className="s-footer-about">
            <Link className="s-footer-brand" to="/">{siteName}</Link>
            <p>{values['footer.summary'] || 'Custom stickers, decals and personalised printed details.'}</p>
          </section>

          <nav aria-label="Footer quick links">
            <h2>Quick links</h2>
            <Link to="/products">Shop</Link>
            <Link to="/custom">Custom stickers</Link>
            <Link to="/about">About</Link>
            <Link to="/contact">Contact</Link>
          </nav>

          {hasContact && (
            <section className="s-footer-contact">
              <h2>Contact</h2>
              {email && safeLink(`mailto:${email}`) && <a href={`mailto:${email}`}><Icon name="mail" />{email}</a>}
              {phone && safeLink(`tel:${phone.replace(/\s/g, '')}`) && <a href={`tel:${phone.replace(/\s/g, '')}`}><Icon name="phone" />{phone}</a>}
              {address && <address><Icon name="mapPin" />{address}</address>}
              {safeLink(whatsapp)?.startsWith('https://') && <a href={whatsapp} target="_blank" rel="noopener noreferrer"><Icon name="arrow" />WhatsApp</a>}
            </section>
          )}

          {socials.length > 0 && (
            <section className="s-footer-social">
              <h2>Stay in touch</h2>
              <p>See owner-posted work and updates on Amaara’s social channels.</p>
              <div className="s-social-links">
                {socials.map(([name, href]) => <SocialLink key={name} name={name} href={href} />)}
              </div>
            </section>
          )}
        </div>
        <div className="s-footer-bottom">
          <span>© {new Date().getFullYear()} {siteName}</span>
        </div>
      </div>
    </footer>
  );
}
