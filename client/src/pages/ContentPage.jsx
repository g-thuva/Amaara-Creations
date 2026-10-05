import { Link, useParams } from 'react-router-dom';
import { contentApi } from '../services/contentApi';
import { useResource } from '../hooks/useResource';
import { useStore } from '../contexts/StoreContext';
import CmsSections from '../components/storefront/CmsSections';
import { EmptyState, ErrorState, PageHeader, Skeleton } from '../components/storefront/UI';
import { safeLink } from '../utils/storefront';
export default function ContentPage({ page }) {
  const { slug } = useParams();
  const current = page || slug;
  const resource = useResource(signal => contentApi.page(current, signal), current);
  const { values, settings } = useStore();
  const contact = current === 'contact';
  const channels = [values['contact.email'] && { href: `mailto:${values['contact.email']}`, text: values['contact.email'], label: 'Email' }, values['contact.phone'] && { href: `tel:${values['contact.phone'].replace(/\s/g, '')}`, text: values['contact.phone'], label: 'Phone' }, ...['instagram', 'facebook', 'tiktok'].map(name => ({ href: values[`social.${name}`], text: name, label: 'Social' }))].filter(item => item && safeLink(item.href));
  return <div className="s-container s-page"><PageHeader eyebrow="Amaara Creations" title={resource.data?.title || (contact ? 'Let’s talk about your idea.' : current === 'about' ? 'Our story' : 'More from Amaara')}>{resource.data?.summary}</PageHeader>{resource.loading ? <Skeleton count={2}/> : resource.error ? <ErrorState error={resource.error} retry={resource.reload}/> : resource.data ? <CmsSections sections={resource.data.sections}/> : !contact && <EmptyState title="More to share soon.">We are preparing this page. In the meantime, explore the shop.</EmptyState>}
    {contact && <section className="s-contact">{settings.error ? <ErrorState error={settings.error} retry={settings.reload}/> : settings.loading ? <Skeleton count={1}/> : <>{channels.length ? <div className="s-contact-grid">{channels.map(item => <a key={item.href} href={safeLink(item.href)} rel="noopener noreferrer"><span className="s-eyebrow">{item.label}</span><strong>{item.text} ↗</strong></a>)}</div> : <p className="s-alert">Contact details are not available yet. Please check back soon.</p>}{values['contact.address'] && <address>{values['contact.address']}</address>}</>}<Link className="s-text-link" to="/products">Explore the shop →</Link></section>}
  </div>;
}
