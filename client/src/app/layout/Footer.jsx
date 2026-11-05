import { useSiteContent } from '@/features/content/SiteContentProvider.jsx';
import { Link } from 'react-router-dom';
import { BrandLogo } from '@/shared/components/ui/BrandLogo.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { safeLink } from '@/shared/utils/safeLink.ts';
import { CONTACT_DETAILS } from '@/app/config/contactDetails.js';

const DEFAULT_INSTAGRAM = CONTACT_DETAILS.instagram;
const DEFAULT_TIKTOK = 'https://www.tiktok.com/@amaaracreations?_t=8qef4xfEYoz&_r=1';
const DEFAULT_PHONE = CONTACT_DETAILS.phone;
const DEFAULT_EMAIL = CONTACT_DETAILS.email;
const DEFAULT_LOCATION = CONTACT_DETAILS.address;

export default function Footer() {
  const { values = {} } = useSiteContent() || {};
  const siteName = values['site.name'] || 'Amaara Creations';

  // Connected to backend site settings, falling back to Switzerland boutique defaults
  const instagramUrl = safeLink(values['social.instagram']) || DEFAULT_INSTAGRAM;
  const tiktokUrl = safeLink(values['social.tiktok']) || DEFAULT_TIKTOK;
  const phone = values['contact.phone'] || DEFAULT_PHONE;
  const email = values['contact.email'] || DEFAULT_EMAIL;
  const address = values['contact.address'] || DEFAULT_LOCATION;

  const privacyUrl = safeLink(values['policy.privacy.url']) || '/help';
  const termsUrl = safeLink(values['policy.terms.url']) || '/help';

  return (
    <footer className="s-footer">
      <div className="s-container">
        <div className="s-footer-main">
          {/* Column 1: Brand, Bio & Big Social Buttons */}
          <section className="s-footer-brand-col">
            <Link className="s-footer-brand" to="/" aria-label={`${siteName} home`}>
              <BrandLogo variant="lockup" tone="light" className="s-footer-brand-logo" />
              <span className="sr-only">{siteName}</span>
            </Link>

            <div className="s-footer-bio">
              <h3 className="s-footer-bio-title">A M A A R A &mdash; Exclusive Welcome Signs</h3>
              <div className="s-footer-bio-story">
                <span className="s-footer-couple" aria-label="husband and wife creating magic">
                  👩🏽❤️👨🏽 Husband &amp; wife creating magic🪄✨
                </span>
                <span className="s-footer-craft">
                  🪧 Welcome Signs &amp; 📄 Personalisierte Stickers
                </span>
              </div>
            </div>

            <div className="s-footer-follow">
              <span className="s-footer-eyebrow">FOLLOW US</span>
              <div className="s-footer-social-buttons" aria-label="Social media links">
                <a
                  href={instagramUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="s-social-btn s-social-btn--instagram"
                  aria-label="Follow Amaara Creations on Instagram"
                >
                  <Icon name="instagram" />
                </a>
                <a
                  href={tiktokUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="s-social-btn s-social-btn--tiktok"
                  aria-label="Follow Amaara Creations on TikTok"
                >
                  <Icon name="tiktok" />
                </a>
              </div>
            </div>
          </section>

          {/* Column 2: Studio & Switzerland Contact */}
          <section className="s-footer-contact-col">
            <span className="s-footer-eyebrow">STUDIO &amp; INQUIRIES</span>
            <ul className="s-footer-contact-list">
              <li>
                <div className="s-contact-icon">
                  <Icon name="mapPin" />
                </div>
                <div className="s-contact-text">
                  <span className="s-contact-label">Location</span>
                  <span className="s-contact-value">{address}</span>
                  <a href={CONTACT_DETAILS.mapUrl} target="_blank" rel="noopener noreferrer" className="s-contact-sub s-contact-link">
                    {CONTACT_DETAILS.plusCode}
                  </a>
                  <small className="s-contact-sub">{CONTACT_DETAILS.hours} · See more hours</small>
                </div>
              </li>
              <li>
                <div className="s-contact-icon">
                  <Icon name="edit" />
                </div>
                <div className="s-contact-text">
                  <span className="s-contact-label">Orders</span>
                  <span className="s-contact-value">📩 DM für personalisierte Bestellungen</span>
                </div>
              </li>
              <li>
                <div className="s-contact-icon">
                  <Icon name="phone" />
                </div>
                <div className="s-contact-text">
                  <span className="s-contact-label">Phone</span>
                  <a href={`tel:${phone.replace(/\s/g, '')}`} className="s-contact-value s-contact-link">
                    {phone}
                  </a>
                </div>
              </li>
              <li>
                <div className="s-contact-icon">
                  <Icon name="mail" />
                </div>
                <div className="s-contact-text">
                  <span className="s-contact-label">Email</span>
                  <a href={`mailto:${email}`} className="s-contact-value s-contact-link">
                    {email}
                  </a>
                </div>
              </li>
            </ul>
          </section>

          {/* Column 3: Navigation Grid */}
          <nav className="s-footer-nav-col" aria-label="Quick links and navigation">
            <span className="s-footer-eyebrow">NAVIGATION &bull; QUICK LINKS</span>
            <div className="s-footer-nav-grid">
              <div className="s-footer-nav-group">
                <Link to="/products" className="s-footer-nav-link">
                  <span>Shop</span>
                  <span className="s-nav-arrow" aria-hidden="true">↗</span>
                </Link>
                <Link to="/custom" className="s-footer-nav-link">
                  <span>Custom Design</span>
                  <span className="s-nav-arrow" aria-hidden="true">↗</span>
                </Link>
                <Link to="/about" className="s-footer-nav-link">
                  <span>About Us</span>
                  <span className="s-nav-arrow" aria-hidden="true">↗</span>
                </Link>
                <Link to="/contact" className="s-footer-nav-link">
                  <span>Contact</span>
                  <span className="s-nav-arrow" aria-hidden="true">↗</span>
                </Link>
              </div>
              <div className="s-footer-nav-group">
                <Link to="/help" className="s-footer-nav-link">
                  <span>Customer Care</span>
                  <span className="s-nav-arrow" aria-hidden="true">↗</span>
                </Link>
                <Link to="/orders" className="s-footer-nav-link">
                  <span>Your Orders</span>
                  <span className="s-nav-arrow" aria-hidden="true">↗</span>
                </Link>
                <Link to="/account/designs" className="s-footer-nav-link">
                  <span>Saved Designs</span>
                  <span className="s-nav-arrow" aria-hidden="true">↗</span>
                </Link>
                {privacyUrl.startsWith('/') ? (
                  <Link to={privacyUrl} className="s-footer-nav-link">
                    <span>Privacy Policy</span>
                    <span className="s-nav-arrow" aria-hidden="true">↗</span>
                  </Link>
                ) : (
                  <a href={privacyUrl} className="s-footer-nav-link" target="_blank" rel="noopener noreferrer">
                    <span>Privacy Policy</span>
                    <span className="s-nav-arrow" aria-hidden="true">↗</span>
                  </a>
                )}
                {termsUrl.startsWith('/') ? (
                  <Link to={termsUrl} className="s-footer-nav-link">
                    <span>Terms &amp; Conditions</span>
                    <span className="s-nav-arrow" aria-hidden="true">↗</span>
                  </Link>
                ) : (
                  <a href={termsUrl} className="s-footer-nav-link" target="_blank" rel="noopener noreferrer">
                    <span>Terms &amp; Conditions</span>
                    <span className="s-nav-arrow" aria-hidden="true">↗</span>
                  </a>
                )}
              </div>
            </div>
          </nav>
        </div>

        {/* Bottom Bar: Copyright & Location Strip */}
        <div className="s-footer-sub">
          <span className="s-footer-copy">
            &copy; {new Date().getFullYear()} {siteName}. All rights reserved.
          </span>
          <span className="s-footer-heart-note">
            Husband &amp; wife creating magic 🪄✨
          </span>
          <span className="s-footer-location">
            {CONTACT_DETAILS.address}
          </span>
        </div>
      </div>
    </footer>
  );
}
