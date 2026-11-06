import { Checkbox } from '@/features/admin/shared/components/AdminUi.jsx';
export function SettingForm({ form, setForm, save, pending, formId }) {
  return <form id={formId} className="add-product-form" onSubmit={save}>
    <fieldset className="admin-form-fields" disabled={pending}>
      <p>{form.description}</p>
      <label>{form.label} <span className="admin-required" aria-hidden="true" />
        {form.format === 'text' ? <textarea className="form-control" required data-dialog-autofocus maxLength={form.maxLength} value={form.value} onChange={e => setForm({ ...form, value: e.target.value })} /> : <input className="form-control" required data-dialog-autofocus type={['email', 'tel', 'url'].includes(form.format) ? form.format : 'text'} maxLength={form.maxLength} value={form.value} onChange={e => setForm({ ...form, value: e.target.value })} />}
      </label>
      {form.format === 'link' && <p className="admin-helper">Use an HTTPS URL or a storefront path beginning with a single slash.</p>}
      <Checkbox label="Show on storefront" description="When disabled, the storefront uses its existing fallback or hides optional content." checked={form.isPublic} onChange={e => setForm({ ...form, isPublic: e.target.checked })} />
    </fieldset>
  </form>;
}
