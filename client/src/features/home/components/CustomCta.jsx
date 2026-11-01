import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';

export default function CustomCta() {
  return (
    <section className="s-custom-cta-section" aria-labelledby="custom-cta-heading">
      <div className="s-container">
        <div className="s-custom-cta-inner">
          <div className="s-custom-cta-copy">
            <p className="s-eyebrow">
              <span className="s-eyebrow-accent" aria-hidden="true" />
              Interactive Studio
            </p>
            <h2 id="custom-cta-heading">
              Ready to design <em>something extraordinary?</em>
            </h2>
            <p className="s-custom-cta-lead">
              Choose your shape, set custom dimensions, add your wording or artwork, and review an instant transparent quote before ordering.
            </p>
          </div>
          <ul className="s-custom-cta-benefits">
            <li><Icon name="check" /> Welcome signs, mirror arches &amp; ceremony boards</li>
            <li><Icon name="check" /> Waterproof die-cut vinyl stickers &amp; labels</li>
            <li><Icon name="check" /> Live shape preview with instant transparent quote</li>
          </ul>
          <div className="s-custom-cta-action">
            <Link className="s-button s-button-light" to="/custom">
              Open Studio <Icon name="arrow" />
            </Link>
            <small>Choose your shape, size, material and text in the interactive builder.</small>
          </div>
        </div>
      </div>
    </section>
  );
}
