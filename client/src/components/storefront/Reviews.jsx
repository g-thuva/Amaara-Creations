import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useResource } from '../../hooks/useResource';
import { useAuth } from '../../contexts/AuthContext';
import { useStore } from '../../contexts/StoreContext';
import { reviewApi } from '../../services/reviewApi';
import { date } from '../../utils/storefront';
import { Button, ConfirmDialog, ErrorState, SectionHeading, Skeleton } from './UI';
export default function Reviews({ productId }) {
  const data = useResource(() => reviewApi.getProductReviews(productId), productId);
  const { user, isAuthenticated } = useAuth();
  const mine = useResource(signal => reviewApi.getOwnReviews(signal, productId), `${user?.id || ''}-${productId}`, isAuthenticated);
  const refresh = () => { data.reload(); mine.reload(); };
  const { mutate, pending } = useStore();
  const [editing, setEditing] = useState(null);
  const [deleting, setDeleting] = useState(null);
  const own = mine.data?.find(r => r.productId === productId);
  const submit = async e => {
    e.preventDefault();
    const form = e.currentTarget;
    const fields = new FormData(form);
    const payload = { rating: Number(fields.get('rating')), comment: fields.get('comment') };
    if (await mutate(() => editing ? reviewApi.updateReview(editing.id, payload) : reviewApi.createReview(productId, payload), refresh, 'Your review is saved and awaiting moderation.')) { setEditing(null); form.reset(); }
  };
  return <section className="s-section s-reviews"><SectionHeading title="Customer reviews"/>{data.loading ? <Skeleton count={2}/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : <>
    <p className="s-lead">{data.data?.totalCount ? `${data.data.averageRating.toFixed(1)} out of 5 · ${data.data.totalCount} reviews` : 'No reviews yet.'}</p>
    <div className="s-review-list">{data.data?.reviews.filter(r => r.id !== own?.id).map(r => <article key={r.id}><div className="s-row"><strong>{r.userName.includes('@') ? 'Customer' : r.userName}</strong><time dateTime={r.createdAt}>{date(r.createdAt)}</time></div><p className="s-stars" aria-label={`${r.rating} out of 5 stars`}>{'★'.repeat(r.rating)}{'☆'.repeat(5 - r.rating)}</p><p>{r.comment}</p>{r.verifiedPurchase && <p>Verified purchase</p>}{user?.id === r.userId && <div className="s-actions"><Button variant="text" disabled={pending} onClick={() => setEditing(r)}>Edit review</Button><Button variant="text" disabled={pending} onClick={() => setDeleting(r.id)}>Delete review</Button></div>}</article>)}</div>
    {mine.error && <ErrorState error={mine.error} retry={mine.reload}/>}
    {own && <article className="s-alert"><h3>Your review · {own.moderationStatus}</h3><p>{own.comment}</p><p>{own.rating} out of 5{own.verifiedPurchase && ' · Verified purchase'}</p><div className="s-actions"><Button variant="text" disabled={pending} onClick={() => setEditing(own)}>Edit review</Button><Button variant="text" disabled={pending} onClick={() => setDeleting(own.id)}>Delete review</Button></div></article>}
    {isAuthenticated && !mine.loading && !mine.error && (!own || editing) && <form key={editing?.id || 'new'} onSubmit={submit} className="s-form s-review-form"><h3>{editing ? 'Edit your review' : 'Share your thoughts'}</h3><label>Rating<select name="rating" defaultValue={editing?.rating || 5}>{[5, 4, 3, 2, 1].map(n => <option key={n} value={n}>{n} stars</option>)}</select></label><label>Your review<textarea name="comment" rows="4" maxLength="1000" defaultValue={editing?.comment || ''}/></label><div className="s-actions"><Button disabled={pending} type="submit">{pending ? 'Saving…' : 'Save review'}</Button>{editing && <Button type="button" variant="text" onClick={() => setEditing(null)}>Cancel</Button>}</div></form>}
    {!isAuthenticated && <Link to="/login" state={{ from: `/products/${productId}` }}>Sign in to write a review ↗</Link>}
  </>}<ConfirmDialog open={!!deleting} title="Delete your review?" pending={pending} onClose={() => setDeleting(null)} onConfirm={async () => { if (await mutate(() => reviewApi.deleteReview(deleting), refresh, 'Review deleted.')) setDeleting(null); }}>This removes your review from the product page.</ConfirmDialog></section>;
}
