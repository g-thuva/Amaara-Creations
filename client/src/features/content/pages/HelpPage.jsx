import '../content.css';
import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';

const questions = [
  ['Where do I start?', 'Browse the published shop or open the sticker designer. Available options come from the current studio configuration.'],
  ['How do I choose a size?', 'Enter the width and height in the measurement unit shown by the designer. The preview follows your proportions. Check the dimensions against the surface where you will use the sticker.'],
  ['Which shapes and materials can I order?', 'The designer lists the options currently enabled by the studio. For a finish, material or special shape that is not listed, ask through a published contact channel before ordering.'],
  ['Which artwork files can I upload?', 'The designer accepts JPEG, PNG and WebP images. Sign in to save artwork privately with your design. A file that fails server validation cannot be used.'],
  ['Can I save an unfinished design?', 'Yes. Save a design to your account and reopen it from Saved designs. A new unsigned design keeps its text and options for the current browser session; artwork requires an account.'],
  ['What does the preview show?', 'It shows your selected shape, proportions, text and artwork. On-screen colour can differ from print. Layout guides are illustrative; final production details belong in the proof.'],
  ['How is the price calculated?', 'The server calculates the current quote from your dimensions, quantity and configured options. It recalculates when saving, adding to the cart and creating an order. Delivery is shown at checkout.'],
  ['Where do I review a proof?', 'When a proof is ready, open the saved design in your account. Review that revision before approving it, or request changes. Proof availability depends on the design workflow.'],
  ['Which delivery and payment methods are available?', 'Checkout shows the methods configured for your order. Availability and charges depend on the current rules. A payment return page alone does not confirm a successful payment.'],
  ['How do I follow my order?', 'Sign in and open Orders for the recorded status and any shipment tracking supplied for your order.'],
  ['What if I need a reprint, special size or bulk quantity?', 'Use the studio’s published contact channels to discuss the request. Confirm suitability, pricing, production timing and any applicable policy before placing an order.'],
];

const quickStarts = [
  {
    icon: 'edit',
    eyebrow: 'Make it yours',
    title: 'Start a custom design',
    copy: 'Choose a size, add your words and build a piece that feels made for your occasion.',
    to: '/custom',
    action: 'Open Design Studio',
  },
  {
    icon: 'layers',
    eyebrow: 'See the collection',
    title: 'Browse ready-made designs',
    copy: 'Explore the currently published collection for a quicker route to a beautiful detail.',
    to: '/products',
    action: 'Visit the shop',
  },
  {
    icon: 'package',
    eyebrow: 'Already ordered?',
    title: 'Find your order',
    copy: 'Sign in to review order status, proofs and any tracking details available for your order.',
    to: '/orders',
    action: 'View orders',
  },
];

export default function HelpPage() {
  return (
    <div className="s-help-page">
      <section className="s-container s-help-start" aria-labelledby="help-start-title">
        <div className="s-help-section-heading">
          <div>
            <p className="s-eyebrow">Choose your path</p>
            <h2 id="help-start-title">Where would you like to begin?</h2>
          </div>
        </div>

        <div className="s-help-start-grid">
          {quickStarts.map((item) => (
            <Link className="s-help-start-card" to={item.to} key={item.title}>
              <span className="s-help-card-icon"><Icon name={item.icon} /></span>
              <span className="s-help-card-eyebrow">{item.eyebrow}</span>
              <h3>{item.title}</h3>
              <p>{item.copy}</p>
              <span className="s-help-card-action">{item.action} <Icon name="arrow" /></span>
            </Link>
          ))}
        </div>
      </section>

      <section className="s-help-answers" id="answers" aria-labelledby="answers-title">
        <div className="s-container s-help-answers-grid">
          <div className="s-help-answers-intro">
            <p className="s-eyebrow">The useful bits</p>
            <h2 id="answers-title">Answers for the moments that matter.</h2>
            <p>Jump into the question you have now. Open another whenever you need it — your place will stay easy to find.</p>
            <div className="s-help-contact-card">
              <span className="s-help-contact-icon"><Icon name="mail" /></span>
              <div>
                <strong>Still not sure?</strong>
                <p>Our studio can help with special sizes, bulk quantities and reprints.</p>
                <Link className="s-text-link" to="/contact">Contact the studio <Icon name="arrow" /></Link>
              </div>
            </div>
          </div>

          <div className="s-help-questions">
            {questions.map(([question, answer], index) => (
              <details className="s-help-question" key={question} open={index === 0}>
                <summary>
                  <span className="s-help-question-number">{String(index + 1).padStart(2, '0')}</span>
                  <span className="s-help-question-title">{question}</span>
                  <span className="s-help-question-toggle" aria-hidden="true"><Icon name="plus" /></span>
                </summary>
                <p>{answer}</p>
              </details>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
