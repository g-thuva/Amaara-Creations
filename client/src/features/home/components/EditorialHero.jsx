import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';

export default function EditorialHero() {
  return <section className="s-editorial-hero" aria-labelledby="studio-title">
    <div className="s-editorial-copy">
      <p className="s-eyebrow">The personal touch, in print</p>
      <h1 id="studio-title">Little details.<br /><em>Entirely yours.</em></h1>
      <p>Custom stickers, decals and printed details for your ideas, your business and your everyday.</p>
      <div className="s-actions"><Link className="s-button s-button-primary" to="/custom">Design your sticker <Icon name="arrow" /></Link><Link className="s-text-link" to="/products">Explore the shop <Icon name="arrow" /></Link></div>
      <p className="s-editorial-caption">Choose a shape. Add your words. Make it personal.</p>
    </div>
    <figure className="s-studio-study">
      <div className="s-study-grid" aria-hidden="true">
        <div className="s-study-sheet"><span>AMAARA<br />CREATIONS</span><div className="s-study-line" /><small>A space for your ideas</small></div>
        <div className="s-study-round">a little<br /><em>more you.</em><span>YOUR WORDS, YOUR WAY</span></div>
        <div className="s-study-label"><small>THE PERSONAL TOUCH</small><strong>made with<br /><em>meaning</em></strong><span>Amaara Creations</span></div>
        <div className="s-study-tag">hello,<br /><em>possibility.</em></div>
        <div className="s-study-swatch"><i /><i /><i /><small>Colour & form</small></div>
      </div>
      <figcaption>Original design studies · illustrative, not product photographs</figcaption>
    </figure>
  </section>;
}
