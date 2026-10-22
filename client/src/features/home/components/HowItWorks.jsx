import Icon from '@/shared/components/ui/Icon.jsx';

const steps = [
  ['search', 'Find your starting point', 'Choose a published product or open the sticker designer.'],
  ['edit', 'Make it your own', 'Set your dimensions, add text or upload artwork privately.'],
  ['check', 'Review the details', 'Check your quote and saved design. Review proofs in your account when requested.'],
  ['bag', 'Continue to checkout', 'Review the available delivery and payment options before placing your order.'],
];

export default function HowItWorks() {
  return (
    <ol className="s-how-grid">
      {steps.map(([icon, title, body], index) => (
        <li key={title}>
          <span className="s-step-icon"><Icon name={icon} /></span>
          <span className="s-step-number">0{index + 1}</span>
          <h3>{title}</h3>
          <p>{body}</p>
        </li>
      ))}
    </ol>
  );
}

