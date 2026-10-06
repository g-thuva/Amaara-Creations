import { Link, useParams } from 'react-router-dom';
import { contentApi } from '../services/contentApi';
import { useResource } from '../hooks/useResource';
import { useStore } from '../contexts/StoreContext';
import CmsSections from '../components/storefront/CmsSections';
import { EmptyState, ErrorState, Icon, PageHeader, Skeleton } from '../components/storefront/UI';
import { safeLink } from '../utils/storefront';

export default function ContentPage({ page }) {
  const { slug } = useParams();
  const current = page || slug;
  const resource = useResource((signal) => contentApi.page(current, signal), current);
  const { values, settings } = useStore();
  const contact = current === 'contact';
  const channels = [
    values['contact.email'] && { href: `mailto:${values['contact.email']}`, text: values['contact.email'], label: 'Email', icon: 'mail' },
    values['contact.phone'] && { href: `tel:${values['contact.phone'].replace(/\s/g, '')}`, text: values['contact.phone'], label: 'Phone', icon: 'phone' },
    (values['contact.whatsapp'] || values['social.whatsapp']) && { href: values['contact.whatsapp'] || values['social.whatsapp'], text: 'Message on WhatsApp', label: 'WhatsApp', icon: 'arrow' },
    ...['instagram', 'facebook', 'tiktok'].map((name) => ({ href: values[`social.${name}`], text: name, label: 'Social', icon: name })),
  ].filter((item) => item && safeLink(item.href));

  return (
    <>
      <div className="s-page-band">
        <div className="s-container">
          <nav className="s-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><Icon name="chevronRight" /><span aria-current="page">{resource.data?.title || current}</span></nav>
          <PageHeader eyebrow="Amaara Creations" title={resource.data?.title || (contact ? 'Contact Amaara' : current === 'about' ? 'About Amaara' : 'More from Amaara')}>
            {resource.data?.summary}
          </PageHeader>
        </div>
      </div>

      <div className="s-container s-page s-content-page">
        {resource.loading ? <Skeleton count={2} /> : resource.error ? (
          <ErrorState error={resource.error} retry={resource.reload} />
        ) : resource.data ? (
          <CmsSections sections={resource.data.sections} />
        ) : !contact && (
          <EmptyState title="This page is being prepared">
            <p>Explore the current product catalogue while this content is completed.</p>
          </EmptyState>
        )}

        {contact && (
          <section className="s-contact">
            {settings.error ? <ErrorState error={settings.error} retry={settings.reload} /> : settings.loading ? <Skeleton count={1} /> : (
              <>
                {channels.length ? (
                  <div className="s-contact-grid">
                    {channels.map((item) => {
                      const external = item.href.startsWith('https://');
                      return (
                        <a key={item.href} href={safeLink(item.href)} target={external ? '_blank' : undefined} rel={external ? 'noopener noreferrer' : undefined}>
                          <Icon name={item.icon} />
                          <span className="s-eyebrow">{item.label}</span>
                          <strong>{item.text}</strong>
                        </a>
                      );
                    })}
                  </div>
                ) : <p className="s-alert">Contact details have not been published yet.</p>}
                {(values['contact.address'] || values['contact.hours']) && (
                  <div className="s-contact-details">
                    {values['contact.address'] && <address><Icon name="mapPin" />{values['contact.address']}</address>}
                    {values['contact.hours'] && <p><Icon name="check" />{values['contact.hours']}</p>}
                  </div>
                )}
              </>
            )}
            <Link className="s-text-link" to="/products">Explore the shop <Icon name="arrow" /></Link>
          </section>
        )}
      </div>
    </>
  );
}
