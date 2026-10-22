import { Link } from 'react-router-dom';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';

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

export default function HelpPage() {
  return <div className="s-container s-page"><nav className="s-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-current="page">Help</span></nav><PageHeader eyebrow="A little guidance" title="From an idea to a printed detail">Practical answers for designing, saving and ordering.</PageHeader><div className="s-help-layout"><aside><h2>Make a start</h2><p>Try a size and your own words in the designer, or browse the currently published collection.</p><div className="s-actions"><Link className="s-button s-button-primary" to="/custom">Open the designer</Link><Link className="s-text-link" to="/contact">Contact the studio</Link></div></aside><div>{questions.map(([question, answer]) => <section className="s-help-question" key={question}><h2>{question}</h2><p>{answer}</p></section>)}</div></div></div>;
}
