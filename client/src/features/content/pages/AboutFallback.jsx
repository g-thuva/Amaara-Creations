import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
import { Media } from '@/shared/components/ui/Media.jsx';

const asset = (path) => `${import.meta.env.BASE_URL}assets/${path}`;

export default function AboutFallback() {
  return (
    <div className="s-about-content">
      <section className="s-about-hero">
        <div className="s-about-hero-copy">
          <span className="s-eyebrow">About Amaara Creations</span>
          <h1>Thoughtful details for the moments that matter.</h1>
          <p>We create welcome signs, personalised stickers and beautifully printed pieces that make celebrations feel unmistakably yours.</p>
          <div className="s-about-actions">
            <Link className="s-button" to="/custom">Start a custom design <Icon name="arrow" /></Link>
            <Link className="s-text-link" to="/products">Explore the shop <Icon name="arrow" /></Link>
          </div>
        </div>
        <Media src={asset('hero/luxury-welcome-sign-hero.jpg')} alt="Elegant welcome sign surrounded by flowers and candlelight" className="s-about-hero-image" width={1366} height={768} eager />
      </section>

      <section className="s-about-feature">
        <Media src={asset('hero/wedding-stationery.jpg')} alt="Minimal wedding stationery arranged on soft fabric" className="s-about-feature-image" width={1400} height={933} />
        <div className="s-about-copy">
          <span className="s-eyebrow">Our approach</span>
          <h2>Designed around your story.</h2>
          <p>The best details are the ones that feel like they could only belong to you. We listen to your idea, help shape the look and create a finished piece that feels warm, considered and ready for its moment.</p>
          <p>From a first sketch to the final print, every Amaara creation is made with patience and a personal touch in our Schaffhausen studio.</p>
        </div>
      </section>

      <section className="s-about-services">
        <div className="s-about-section-heading">
          <div><span className="s-eyebrow">What we create</span><h2>Small details. Big feeling.</h2></div>
        </div>
        <div className="s-about-service-grid">
          <article className="s-about-service-card"><Media src={asset('products/wedding-direction-arrow.jpg')} alt="Personalised wedding direction sign" width={1024} height={1024} /><div><h3>Welcome signs</h3><p>Make the first impression feel personal with a sign created for your celebration.</p></div></article>
          <article className="s-about-service-card"><Media src={asset('hero/wedding-stationery.jpg')} alt="Elegant printed wedding stationery" width={1400} height={933} /><div><h3>Printed details</h3><p>Menus, stationery and finishing touches that bring your table and event together.</p></div></article>
          <article className="s-about-service-card"><Media src={asset('products/table-numbers-stationery.jpg')} alt="Printed table number cards in a garden setting" width={1024} height={1024} /><div><h3>Personalised stickers</h3><p>Thoughtful labels and stickers that add your name, message or brand to every detail.</p></div></article>
        </div>
      </section>

    </div>
  );
}
