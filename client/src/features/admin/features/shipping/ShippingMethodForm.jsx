import { Checkbox } from '@/features/admin/shared/components/AdminUi.jsx';

export function ShippingMethodForm({ value: method, setValue, resource, formId, pending, save }) {
  const update = (field, value) => setValue({ ...method, [field]: value });
  const numberField = (field, label, integer = false) => <label key={field}>{label}{field === 'basePrice' && <span className="admin-required" aria-hidden="true" />}<input className="form-control" type="number" min="0" max={integer ? field === 'displayOrder' ? 2147483647 : 365 : undefined} step={integer ? 1 : 0.01} required={field === 'basePrice'} value={method[field] ?? ''} onChange={e => update(field, e.target.value === '' ? null : Number(e.target.value))} /></label>;
  return <form id={formId} onSubmit={save} className="add-product-form">
    <fieldset className="admin-form-fields" disabled={pending}>
      <section className="admin-form-section" aria-labelledby="method-basic">
        <h3 id="method-basic">Basic details</h3>
        <div className="form-row">
          <label>Delivery zone <span className="admin-required" aria-hidden="true" /><select className="form-control" required data-dialog-autofocus value={method.shippingZoneId || ''} onChange={e => update('shippingZoneId', e.target.value)}><option value="">Choose a zone</option>{resource.data?.zones.map(zone => <option key={zone.id} value={zone.id}>{zone.name}{!zone.isActive && ' (inactive)'}</option>)}</select></label>
          <label>Name <span className="admin-required" aria-hidden="true" /><input className="form-control" required maxLength={120} value={method.name} onChange={e => update('name', e.target.value)} /></label>
          <label>Code <span className="admin-required" aria-hidden="true" /><input className="form-control" required maxLength={50} pattern="[A-Z0-9_-]+" title="Use uppercase letters, numbers, underscores or hyphens." value={method.code} onChange={e => update('code', e.target.value)} /></label>
          <label>Currency <span className="admin-required" aria-hidden="true" /><input className="form-control" required maxLength={3} pattern="[A-Z]{3}" value={method.currency} onChange={e => update('currency', e.target.value)} /><small className="admin-helper">Use the currency approved for this delivery method.</small></label>
        </div>
      </section>
      <section className="admin-form-section" aria-labelledby="method-pricing">
        <h3 id="method-pricing">Pricing</h3>
        <p className="admin-helper">Enter approved rates only. Leave optional thresholds blank when they do not apply.</p>
        <div className="form-row">{numberField('basePrice', 'Base price')}{numberField('freeShippingThreshold', 'Free-shipping threshold')}{numberField('minimumOrderValue', 'Minimum order value')}{numberField('maximumOrderValue', 'Maximum order value')}</div>
      </section>
      <section className="admin-form-section" aria-labelledby="method-estimates">
        <h3 id="method-estimates">Estimated delivery</h3>
        <div className="form-row">{numberField('estimatedMinimumDays', 'Minimum delivery days', true)}{numberField('estimatedMaximumDays', 'Maximum delivery days', true)}</div>
      </section>
      <section className="admin-form-section" aria-labelledby="method-availability">
        <h3 id="method-availability">Availability</h3>
        {numberField('displayOrder', 'Display order', true)}
        <div className="admin-check-grid">
          <Checkbox label="Active method" checked={method.isActive} onChange={e => update('isActive', e.target.checked)} />
          <Checkbox label="Customer pickup" checked={method.isPickup} onChange={e => update('isPickup', e.target.checked)} />
          <Checkbox label="Test rate (excluded from regular checkout)" description="Production checkout continues to exclude development rates." checked={method.isDevelopmentValue} onChange={e => update('isDevelopmentValue', e.target.checked)} />
        </div>
      </section>
      {method.isPickup && <section className="admin-form-section" aria-labelledby="method-pickup"><h3 id="method-pickup">Pickup configuration</h3><label>Pickup location <span className="admin-required" aria-hidden="true" /><input className="form-control" required maxLength={500} value={method.pickupLocation || ''} onChange={e => update('pickupLocation', e.target.value)} /></label></section>}
    </fieldset>
  </form>;
}
