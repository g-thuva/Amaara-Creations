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
import { CONTACT_DETAILS } from '@/app/config/contactDetails.js';
import AboutFallback from '@/features/content/pages/AboutFallback.jsx';

export default function ContentPage({ page }) {
  const { slug } = useParams();
  const current = page || slug;
  const resource = useResource((signal) => contentApi.page(current, signal), current);
  const { settings, values } = useSiteContent();
  const about = current === 'about';
  const contact = current === 'contact';
  const missing = !page && !resource.loading && !resource.error && !resource.data;
  const contactEmail = values['contact.email'] || CONTACT_DETAILS.email;
  const contactPhone = values['contact.phone'] || CONTACT_DETAILS.phone;
  const contactInstagram = values['social.instagram'] || CONTACT_DETAILS.instagram;
  const contactAddress = values['contact.address'] || CONTACT_DETAILS.address;
  const contactHours = values['contact.hours'] || CONTACT_DETAILS.hours;
  const channels = [
    { href: `mailto:${contactEmail}`, text: contactEmail, label: 'Email', icon: 'mail' },
    { href: `tel:${contactPhone.replace(/\s/g, '')}`, text: contactPhone, label: 'Phone', icon: 'phone' },
    (values['contact.whatsapp'] || values['social.whatsapp']) && { href: values['contact.whatsapp'] || values['social.whatsapp'], text: 'Message on WhatsApp', label: 'WhatsApp', icon: 'arrow' },
    { href: contactInstagram, text: 'instagram', label: 'Social', icon: 'instagram' },
    ...['facebook', 'tiktok'].map((name) => ({ href: values[`social.${name}`], text: name, label: 'Social', icon: name })),
  ].filter((item) => item && safeLink(item.href));

  return (
    <>
      {resource.data && !resource.loading && <ContentSeo page={resource.data} path={page ? `/${page}` : `/pages/${encodeURIComponent(current)}`} />}
      {!about && <div className="s-page-band">
        <div className="s-container">
          <nav className="s-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><Icon name="chevronRight" /><span aria-current="page">{resource.data?.title || current}</span></nav>
          <PageHeader eyebrow="Amaara Creations" title={resource.data?.title || (missing ? 'Page not found' : contact ? 'Contact Amaara' : 'More from Amaara')}>
            {resource.data?.summary}
          </PageHeader>
        </div>
      </div>}

      <div className={`s-container s-page s-content-page ${about ? 's-about-page' : ''}`}>
        {resource.loading ? <Skeleton count={2} /> : resource.error ? (
          <ErrorState error={resource.error} retry={resource.reload} />
        ) : resource.data ? (
          <CmsSections sections={resource.data.sections} />
        ) : about ? (
          <AboutFallback />
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
                {(contactAddress || contactHours) && (
                  <div className="s-contact-details">
                    <address><Icon name="mapPin" />{contactAddress}</address>
                    <p><Icon name="mapPin" /><a href={CONTACT_DETAILS.mapUrl} target="_blank" rel="noopener noreferrer">{CONTACT_DETAILS.plusCode}</a></p>
                    <p><Icon name="check" />{contactHours} · <a href={CONTACT_DETAILS.mapUrl} target="_blank" rel="noopener noreferrer">See more hours</a></p>
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
