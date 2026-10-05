import { Link } from 'react-router-dom';
import { useStore } from '../contexts/StoreContext';
import { safeLink } from '../utils/storefront';
export default function Footer() {
  const { values } = useStore();
  return <footer className="s-footer"><div className="s-container">
    <div className="s-footer-top"><div><Link className="s-footer-brand" to="/">{values['site.name'] || 'Amaara Creations'}<span aria-hidden="true">✳</span></Link>{values['footer.summary'] && <p>{values['footer.summary']}</p>}</div>
      <nav aria-label="Footer shop"><h2>Make it yours</h2><Link to="/products">Shop all products</Link><Link to="/custom">Custom stickers</Link><Link to="/wishlist">Your wishlist</Link></nav>
      <nav aria-label="Customer help"><h2>Here to help</h2><Link to="/about">Our story</Link><Link to="/contact">Contact</Link><Link to="/profile">Your account</Link><Link to="/orders">Your orders</Link></nav>
      <div className="s-footer-contact"><h2>Stay in touch</h2>{values['contact.email'] && safeLink(`mailto:${values['contact.email']}`) && <a href={`mailto:${values['contact.email']}`}>{values['contact.email']}</a>}{values['contact.phone'] && safeLink(`tel:${values['contact.phone'].replace(/\s/g, '')}`) && <a href={`tel:${values['contact.phone'].replace(/\s/g, '')}`}>{values['contact.phone']}</a>}{['instagram', 'facebook', 'tiktok'].map(name => safeLink(values[`social.${name}`]) && <a key={name} href={safeLink(values[`social.${name}`])} target="_blank" rel="noopener noreferrer">{name}</a>)}<Link to="/contact">Contact options ↗</Link></div>
    </div><div className="s-footer-bottom"><span>© {new Date().getFullYear()} {values['site.name'] || 'Amaara Creations'}</span><span>Little details. Personal expression.</span></div>
  </div></footer>;
}
