import { useSavedDesigns } from '@/features/custom-design/hooks/useSavedDesigns.js';
import SavedDesignCard from '@/features/custom-design/components/SavedDesignCard.jsx';
import { Link } from 'react-router-dom';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';

export default function SavedDesignsPage() {
  const { designs, pending, duplicate, archive, add } = useSavedDesigns();
  return (
    <>
      <PageHeader eyebrow="Reusable designs" title="Saved designs">Reopen, duplicate or order a custom sticker design.</PageHeader>
      {designs.loading ? <Skeleton count={3} /> : designs.error ? <ErrorState error={designs.error} retry={designs.reload} /> : !designs.data?.items?.length ? (
        <EmptyState title="No saved designs yet" to="/custom" label="Build a custom sticker" />
      ) : <div className="s-design-grid">{designs.data.items.map((design) => (
        <SavedDesignCard key={design.id} design={design} pending={pending} duplicate={duplicate} archive={archive} add={add} />
      ))}</div>}
      <Link className="s-text-link" to="/custom">Create another design <Icon name="arrow" /></Link>
    </>
  );
}
