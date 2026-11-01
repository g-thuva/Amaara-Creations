import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';

const heroAssetPath = `${import.meta.env.BASE_URL}assets/hero`;

export default function EditorialHero() {
  return (
    <section className="s-editorial-hero" aria-labelledby="studio-title">
      <img
        className="s-editorial-background"
        src={`${heroAssetPath}/luxury-welcome-sign-hero.jpg`}
        onError={(e) => {
          // Fallback if luxury welcome sign is not found
          e.currentTarget.src = `${heroAssetPath}/wedding-stationery.jpg`;
        }}
        alt="Amaara Creations exclusive wedding welcome signs and custom prints"
        width="1376"
        height="768"
        loading="eager"
        decoding="async"
      />
      <span className="s-editorial-scrim" aria-hidden="true" />
      <div className="s-container s-editorial-container">
        <div className="s-editorial-copy">
          <p className="s-eyebrow">
            <span className="s-eyebrow-accent" aria-hidden="true" />
            Exclusive Wedding &amp; Event Signs
          </p>
          <h1 id="studio-title">
            Little details.<br />
            <em>Entirely yours.</em>
          </h1>
          <p className="s-editorial-lead">
            Handcrafted acrylic welcome signs, mirror arches, luxury event stationery and personalised waterproof stickers — designed with love for weddings, milestones and your most memorable moments.
          </p>
          <div className="s-actions">
            <Link className="s-button s-button-primary s-hero-cta" to="/custom">
              Design your sticker <Icon name="arrow" />
            </Link>
            <Link className="s-button s-button-glass s-hero-secondary" to="/products">
              Explore the shop <Icon name="arrow" />
            </Link>
          </div>
          <div className="s-editorial-trust" aria-label="Amaara Creations craft qualities">
            <span><Icon name="check" />Handcrafted Acrylic &amp; Mirror Signs</span>
            <span><Icon name="check" />Waterproof Vinyl Stickers</span>
            <span><Icon name="check" />Personalised to Perfection</span>
          </div>
        </div>
      </div>
    </section>
  );
}
