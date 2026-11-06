export function ReviewModerationForm({ formId, intent, reason, setReason, pending, confirm }) {
  return <form id={formId} className="add-product-form" onSubmit={confirm}>
    <fieldset className="admin-form-fields" disabled={pending}>
      <div className="admin-review-summary"><strong>{intent.productName} · {intent.userName}</strong><p>{intent.rating} out of 5 · Current state: {intent.moderationStatus}</p><p className="admin-review-comment">{intent.comment || 'No written comment.'}</p></div>
      <p>{intent.status === 'Hidden' ? 'This review will disappear from the public review list and rating summaries. An authorized operator can restore it.' : intent.status === 'Removed' ? 'This review will be removed from public display and ratings. Its record and moderation history are retained. The customer cannot republish it by editing or resubmitting.' : 'This review will appear publicly and contribute to product ratings again.'}</p>
      <label>Moderation reason <span className="admin-required" aria-hidden="true" /><textarea className="form-control" required data-dialog-autofocus maxLength={500} value={reason} onChange={e => setReason(e.target.value)} /></label>
    </fieldset>
  </form>;
}
