// A successful HTTP request may still contain a pending or failed refund.
// Only the backend's refund status can establish completion.
export function refundNotice(refund) {
  if (refund?.status === 'Completed') return { success: true, text: 'Request status: Completed.' };
  if (refund?.status === 'Pending') return {
    text: 'Request status: Pending.',
    detail: 'Awaiting provider verification. Reconcile this refund or review it in the merchant dashboard before submitting another request.',
  };
  if (refund?.status === 'Failed') return {
    error: true,
    text: 'Request status: Failed.',
    detail: 'No refund was completed. Check the payment details and provider result before trying again.',
  };
  return {
    error: true,
    text: 'Refund status could not be verified.',
    detail: 'Refresh the payment details and review refund history before trying again.',
  };
}
