import { Button } from '@/shared/components/ui/Button.jsx';
import { formatDate as date } from '@/shared/utils/formatDate.ts';
export default function ReviewCard({ review: r, user, pending, setEditing, setDeleting }) {
  return (<article key={r.id}><div className="s-row"><strong>{r.userName.includes('@') ? 'Customer' : r.userName}</strong><time dateTime={r.createdAt}>{date(r.createdAt)}</time></div><p className="s-stars" aria-label={`${r.rating} out of 5 stars`}>{'★'.repeat(r.rating)}{'☆'.repeat(5 - r.rating)}</p><p>{r.comment}</p>{r.verifiedPurchase && <p>Verified purchase</p>}{user?.id === r.userId && <div className="s-actions"><Button variant="text" disabled={pending} onClick={() => setEditing(r)}>Edit review</Button><Button variant="text" disabled={pending} onClick={() => setDeleting(r.id)}>Delete review</Button></div>}</article>);
}
