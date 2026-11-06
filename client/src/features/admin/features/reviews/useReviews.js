import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { useAdminListParams } from '@/features/admin/shared/hooks/useAdminListParams.js';
import { reviewsApi } from './reviewsApi.js';
export function useReviews() {
  const { params, setPage, setFilters } = useAdminListParams({ status: '', search: '', rating: '', productId: '' });
  const [intent, setIntent] = useState(null);
  const [reason, setReason] = useState('');
  const { page, status, search, rating, productId } = params;
  const resource = useResource(signal => reviewsApi.listReviews({ pageNumber: page, status: status || undefined, search: search || undefined, rating: rating || undefined, productId: productId || undefined }, signal), JSON.stringify(params));
  const mutation = useAdminMutation(resource.reload);
  const moderate = (row, next) => { mutation.reset(); setReason(''); setIntent({ ...row, status: next }); };
  const confirm = async event => {
    event.preventDefault();
    if (resource.loading || resource.error) return;
    if (await mutation.run(() => reviewsApi.moderateReview(intent.id, { status: intent.status, rowVersion: intent.rowVersion, reason: reason.trim() }), 'Review moderation recorded. Public ratings use published reviews only.')) setIntent(null);
  };
  return { ...params, setPage, setFilters, resource, mutation, intent, setIntent, reason, setReason, moderate, confirm };
}
