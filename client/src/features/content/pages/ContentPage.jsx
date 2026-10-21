import { useSiteContent } from '@/features/content/SiteContentProvider.jsx';
import { Link, useParams } from 'react-router-dom';
import { contentApi } from '@/features/content/contentApi.js';
import { useResource } from '@/shared/hooks/useResource.js';
import CmsSections from '@/features/content/components/CmsSections.jsx';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import { safeLink } from '@/shared/utils/safeLink.ts';
import ContentSeo from '@/features/content/components/ContentSeo.jsx';

export default function ContentPage({ page }) {
  const { slug } = useParams();
  const current = page || slug;
  const resource = useResource((signal) => contentApi.page(current, signal), current);
  const { settings, values } = useSiteContent();
  const contact = current === 'contact';
  const missing = !page && !resource.loading && !resource.error && !resource.data;
  const channels = [
    values['contact.email'] && { href: `mailto:${values['contact.email']}`, text: values['contact.email'], label: 'Email', icon: 'mail' },
    values['contact.phone'] && { href: `tel:${values['contact.phone'].replace(/\s/g, '')}`, text: values['contact.phone'], label: 'Phone', icon: 'phone' },
    (values['contact.whatsapp'] || values['social.whatsapp']) && { href: values['contact.whatsapp'] || values['social.whatsapp'], text: 'Message on WhatsApp', label: 'WhatsApp', icon: 'arrow' },
    ...['instagram', 'facebook', 'tiktok'].map((name) => ({ href: values[`social.${name}`], text: name, label: 'Social', icon: name })),
  ].filter((item) => item && safeLink(item.href));

  return (
    <>
      {resource.data && !resource.loading && <ContentSeo page={resource.data} path={page ? `/${page}` : `/pages/${encodeURIComponent(current)}`} />}
      <div className="s-page-band">
        <div className="s-container">
          <nav className="s-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><Icon name="chevronRight" /><span aria-current="page">{resource.data?.title || current}</span></nav>
          <PageHeader eyebrow="Amaara Creations" title={resource.data?.title || (missing ? 'Page not found' : contact ? 'Contact Amaara' : current === 'about' ? 'About Amaara' : 'More from Amaara')}>
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
          <EmptyState to="/custom" label="Explore custom design" title={missing ? 'This page is not available' : 'A personal touch, in print'}>
            <p>{missing ? 'The link may be old, or the page may no longer be published.' : 'Explore custom stickers and printed details through the designer and the currently published shop. For help with files and ordering, visit Help.'}</p>
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
