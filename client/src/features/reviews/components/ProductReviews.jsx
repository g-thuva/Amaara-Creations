import { useProductReviews } from '@/features/reviews/hooks/useProductReviews.js';
import ReviewForm from './ReviewForm.jsx';
import ReviewCard from './ReviewCard.jsx';
import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { SectionHeading } from '@/shared/components/ui/SectionHeading.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
export default function ProductReviews({ productId }) {
  const { data, mine, user, isAuthenticated, own, pending, editing, setEditing, deleting, setDeleting, submit, remove } = useProductReviews(productId);
  return <section className="s-section s-reviews"><SectionHeading title="Customer reviews"/>{data.loading ? <Skeleton count={2}/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : <>
    <p className="s-lead">{data.data?.totalCount ? `${data.data.averageRating.toFixed(1)} out of 5 · ${data.data.totalCount} reviews` : 'No reviews yet.'}</p>
    <div className="s-review-list">{data.data?.reviews.filter(r => r.id !== own?.id).map(r => <ReviewCard key={r.id} review={r} user={user} pending={pending} setEditing={setEditing} setDeleting={setDeleting} />)}</div>
    {mine.error && <ErrorState error={mine.error} retry={mine.reload}/>}
    {own && <article className="s-alert"><h3>Your review · {own.moderationStatus}</h3><p>{own.comment}</p><p>{own.rating} out of 5{own.verifiedPurchase && ' · Verified purchase'}</p><div className="s-actions"><Button variant="text" disabled={pending} onClick={() => setEditing(own)}>Edit review</Button><Button variant="text" disabled={pending} onClick={() => setDeleting(own.id)}>Delete review</Button></div></article>}
    {isAuthenticated && !mine.loading && !mine.error && (!own || editing) && <ReviewForm key={editing?.id || "new"} editing={editing} pending={pending} submit={submit} setEditing={setEditing} />}
    {!isAuthenticated && <Link to="/login" state={{ from: `/products/${productId}` }}>Sign in to write a review ↗</Link>}
  </>}<ConfirmDialog open={!!deleting} title="Delete your review?" pending={pending} onClose={() => setDeleting(null)} onConfirm={remove}>This removes your review from the product page.</ConfirmDialog></section>;
}
