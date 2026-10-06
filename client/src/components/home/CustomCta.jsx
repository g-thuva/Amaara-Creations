import { Link } from 'react-router-dom';
import { Icon } from '../storefront/UI';

export default function CustomCta() {
  return (
    <section className="s-custom-cta">
      <div className="s-custom-motif" aria-hidden="true"><span /><span /><span /></div>
      <div>
        <p className="s-eyebrow">Custom sticker preview</p>
        <h2>Ready to create something personal?</h2>
        <p>Try your wording and proportions before contacting Amaara about production.</p>
      </div>
      <ul>
        <li><Icon name="check" /> Enter your own text</li>
        <li><Icon name="check" /> Compare type styles</li>
        <li><Icon name="check" /> Adjust the sticker size</li>
      </ul>
      <div>
        <Link className="s-button s-button-light" to="/custom">Start designing <Icon name="arrow" /></Link>
        <small>Preview only. Final pricing and ordering require contact.</small>
      </div>
    </section>
  );
}

