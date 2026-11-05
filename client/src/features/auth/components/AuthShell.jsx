import '@/features/auth/auth.css';
import { BrandLogo } from '@/shared/components/ui/BrandLogo.jsx';

const authImage = `${import.meta.env.BASE_URL}assets/hero/luxury-welcome-sign-hero.jpg`;

export default function AuthShell({ children, security = false }) {
  return (
    <div className="auth-container">
      <aside className="auth-brand-panel">
        <img className="auth-brand-image" src={authImage} alt="Amaara welcome sign surrounded by flowers and candlelight" />
        <div className="auth-brand-overlay" aria-hidden="true" />
        <div className="auth-brand-content">
          <BrandLogo variant="lockup" tone="light" className="auth-brand-logo" />
          <p className="s-eyebrow">Amaara Creations</p>
          <h2>{security ? 'A little care for everything you create.' : 'Beautiful details, made for your moments.'}</h2>
          <p>{security ? 'A secure sign-in keeps your account and personal details protected.' : 'Welcome back to your personal space for thoughtful printed details.'}</p>
        </div>
      </aside>
      {children}
    </div>
  );
}
