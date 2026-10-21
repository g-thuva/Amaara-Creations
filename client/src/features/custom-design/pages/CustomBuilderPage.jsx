import { useParams } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import { artworkError } from '@/features/custom-design/artworkActions.js';
import { useCustomBuilder } from '@/features/custom-design/hooks/useCustomBuilder.js';
import BuilderControls from '@/features/custom-design/components/BuilderControls.jsx';
import BuilderPreview from '@/features/custom-design/components/BuilderPreview.jsx';

export default function CustomBuilderPage() {
  const { id } = useParams();
  const builder = useCustomBuilder(id);
  const { loading, error, config, design, quote, busy, addToCart } = builder;
  if (loading) return <div className="s-container s-page"><Skeleton count={3} /></div>;
  if (error && !config) return <div className="s-container s-page"><ErrorState error={error} retry={builder.reload} /></div>;

  return (
    <div className="s-container s-page s-custom-page">
      <PageHeader eyebrow="Custom sticker builder" title={design ? `Edit ${design.name}` : 'Build a custom sticker'}>
        Choose your shape and size, add your text or artwork, and review a quote before saving. Uploaded artwork stays private in your account.
      </PageHeader>
      {error && <p className="s-error-text" role="alert">{artworkError(error)}</p>}
      <div className="s-builder">
        <BuilderControls builder={builder} />
        <BuilderPreview builder={builder} />
      </div>
      <div className="s-builder-sticky"><span>{quote ? <Price value={quote.subtotal} currency={quote.currency} /> : 'Quote required'}</span><Button type="button" disabled={busy || !quote} onClick={addToCart}>Add custom design</Button></div>
    </div>
  );
}
