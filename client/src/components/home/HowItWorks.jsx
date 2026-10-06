import { Icon } from '../storefront/UI';

const steps = [
  ['search', 'Choose or customise', 'Browse available products or try the custom sticker preview.'],
  ['bag', 'Add to cart', 'Add an available standard product in the quantity you need.'],
  ['package', 'We prepare your order', 'Amaara receives the recorded order and prepares the items.'],
  ['truck', 'Delivered to you', 'Delivery details are arranged for the order you placed.'],
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

