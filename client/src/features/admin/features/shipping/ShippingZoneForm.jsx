import { Checkbox } from '@/features/admin/shared/components/AdminUi.jsx';

export function ShippingZoneForm({ value: zone, setValue, formId, pending, save }) {
  const update = (field, value) => setValue({ ...zone, [field]: value });
  return <form id={formId} onSubmit={save} className="add-product-form">
    <fieldset className="admin-form-fields" disabled={pending}>
      <section className="admin-form-section" aria-labelledby="zone-general">
        <h3 id="zone-general">General information</h3>
        <div className="form-row">
          <label>Name <span className="admin-required" aria-hidden="true" /><input className="form-control" required data-dialog-autofocus maxLength={120} value={zone.name} onChange={e => update('name', e.target.value)} /></label>
          <label>Country <span className="admin-required" aria-hidden="true" /><input className="form-control" required maxLength={80} value={zone.country} onChange={e => update('country', e.target.value)} /></label>
        </div>
        <label>Region<input className="form-control" maxLength={100} value={zone.region || ''} onChange={e => update('region', e.target.value)} /></label>
      </section>
      <section className="admin-form-section" aria-labelledby="zone-address">
        <h3 id="zone-address">Address requirements</h3>
        <label>Postal code prefix<input className="form-control" maxLength={30} value={zone.postalCodePrefix || ''} onChange={e => update('postalCodePrefix', e.target.value)} /><small className="admin-helper">Leave blank to accept all postal codes in this zone.</small></label>
        <div className="admin-check-grid">
          <Checkbox label="Require phone number" checked={zone.requiresPhone} onChange={e => update('requiresPhone', e.target.checked)} />
          <Checkbox label="Require region" checked={zone.requiresRegion} onChange={e => update('requiresRegion', e.target.checked)} />
          <Checkbox label="Require postal code" checked={zone.requiresPostalCode} onChange={e => update('requiresPostalCode', e.target.checked)} />
        </div>
      </section>
      <section className="admin-form-section" aria-labelledby="zone-availability">
        <h3 id="zone-availability">Availability</h3>
        <Checkbox label="Active zone" description="Only active zones can offer delivery options at checkout." checked={zone.isActive} onChange={e => update('isActive', e.target.checked)} />
      </section>
    </fieldset>
  </form>;
}
