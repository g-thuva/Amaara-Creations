import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { reviewsApi } from '@/features/reviews/reviewsApi.js';

export function useProductReviews(productId) {
  const data = useResource(() => reviewsApi.getProductReviews(productId), productId);
  const { user, isAuthenticated } = useAuth();
  const mine = useResource(signal => reviewsApi.getOwnReviews(signal, productId), `${user?.id || ''}-${productId}`, isAuthenticated);
  const refresh = () => { data.reload(); mine.reload(); };
  const { mutate, pending } = useAuthenticatedMutation();
  const [editing, setEditing] = useState(null);
  const [deleting, setDeleting] = useState(null);
  const own = mine.data?.find(r => r.productId === productId);
  const submit = async e => {
    e.preventDefault();
    const form = e.currentTarget;
    const fields = new FormData(form);
    const payload = { rating: Number(fields.get('rating')), comment: fields.get('comment') };
    if (await mutate(() => editing ? reviewsApi.updateReview(editing.id, payload) : reviewsApi.createReview(productId, payload), refresh, 'Your review is saved and awaiting moderation.')) { setEditing(null); form.reset(); }
  };
  const remove = async () => { if (await mutate(() => reviewsApi.deleteReview(deleting), refresh, 'Review deleted.')) setDeleting(null); };
  return { data, mine, user, isAuthenticated, own, pending, editing, setEditing, deleting, setDeleting, submit, remove };
}
