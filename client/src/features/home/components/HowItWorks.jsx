import { Link } from 'react-router-dom';

const steps = [
  {
    number: '1',
    title: 'Choose your design',
    description: 'Browse our curated collection of mirror arches, acrylic welcome boards, and personalised stationery.',
    icon: (
      <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <circle cx="11" cy="11" r="8" />
        <line x1="21" y1="21" x2="16.65" y2="16.65" />
      </svg>
    ),
  },
  {
    number: '2',
    title: 'Send your request',
    description: 'Select your preferred dates, wording and style. We review your details and confirm immediately.',
    icon: (
      <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <line x1="22" y1="2" x2="11" y2="13" />
        <polygon points="22 2 15 22 11 13 2 9 22 2" />
      </svg>
    ),
  },
  {
    number: '3',
    title: 'Collect and celebrate',
    description: 'Arrive at our Switzerland studio, receive your bespoke creations ready for your celebration, and make memories.',
    icon: (
      <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <circle cx="12" cy="12" r="9" />
        <polyline points="8.5 12 11 14.5 15.5 9.5" />
      </svg>
    ),
  },
];

export default function HowItWorks() {
  return (
    <section className="s-section s-how-section" aria-labelledby="how-it-works-heading">
      <div className="s-how-header">
        <span className="s-how-eyebrow">SEAMLESS PROCESS</span>
        <h2 id="how-it-works-heading">How it works</h2>
        <p className="s-how-subtitle">
          Three simple steps between your vision and your perfect celebration.
        </p>
      </div>

      <ol className="s-how-timeline">
        {steps.map((step) => (
          <li key={step.number} className="s-how-step">
            <div className="s-how-circle">
              {step.icon}
              <span className="s-how-badge" aria-label={`Step ${step.number}`}>
                {step.number}
              </span>
            </div>
            <h3 className="s-how-step-title">{step.title}</h3>
            <p className="s-how-step-desc">{step.description}</p>
          </li>
        ))}
      </ol>

      <div className="s-how-cta-wrap">
        <Link to="/custom" className="s-how-cta">
          START YOUR DESIGN REQUEST
        </Link>
      </div>
    </section>
  );
}
