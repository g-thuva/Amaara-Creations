import { useState } from 'react';
import { useResource } from '../../hooks/useResource';
import { checkoutAdminApi } from '../../services/checkoutAdminApi';

const newZone = () => ({ name: '', country: '', region: '', postalCodePrefix: '', isActive: true, requiresPhone: true, requiresRegion: false, requiresPostalCode: false });
const newMethod = () => ({ name: '', code: '', shippingZoneId: '', currency: 'LKR', basePrice: 0, freeShippingThreshold: null, minimumOrderValue: null, maximumOrderValue: null, estimatedMinimumDays: null, estimatedMaximumDays: null, displayOrder: 0, isActive: true, isPickup: false, pickupLocation: '', isDevelopmentValue: true });
const numeric = ['basePrice', 'freeShippingThreshold', 'minimumOrderValue', 'maximumOrderValue', 'estimatedMinimumDays', 'estimatedMaximumDays', 'displayOrder'];

export default function Shipping() {
  const resource = useResource(checkoutAdminApi.shipping, 'shipping');
  const [zone, setZone] = useState(newZone);
  const [method, setMethod] = useState(newMethod);
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState(null);
  const save = async (event, type) => {
    event.preventDefault(); if (pending) return; setPending(true); setMessage(null);
    try {
      if (type === 'zone') await checkoutAdminApi.saveZone(zone);
      else await checkoutAdminApi.saveMethod({ ...method, shippingZoneId: Number(method.shippingZoneId) });
      resource.reload(); setMessage({ success: true, text: 'Shipping configuration saved.' });
    } catch (e) { setMessage({ text: e.response?.data?.title || 'Shipping configuration could not be saved.' }); }
    finally { setPending(false); }
  };
  return <div className="admin-page">
    <div className="page-header"><h2>Shipping zones and methods</h2></div>
    <p>Enter business-approved rates, regions, delivery estimates and pickup locations. Development values are excluded from checkout unless explicitly enabled in Development.</p>
    {resource.loading && <p role="status">Loading shipping configuration…</p>}{resource.error && <p role="alert">Shipping configuration could not be loaded.</p>}{message && <p role={message.success ? 'status' : 'alert'}>{message.text}</p>}
    <section className="card"><div className="card-header"><h3>Delivery zones</h3><button className="btn btn-outline-primary" onClick={() => setZone(newZone())}>New zone</button></div><div className="card-body">
      <label>Edit zone<select className="form-control" value={zone.id || ''} onChange={e => setZone(resource.data.zones.find(z => z.id === Number(e.target.value)) || newZone())}><option value="">New zone</option>{resource.data?.zones.map(z => <option key={z.id} value={z.id}>{z.name}</option>)}</select></label>
      <form onSubmit={e => save(e, 'zone')} className="add-product-form"><div className="form-row">{['name', 'country', 'region', 'postalCodePrefix'].map(field => <label key={field}>{field.replace(/([A-Z])/g, ' $1')}<input className="form-control" required={['name', 'country'].includes(field)} value={zone[field] || ''} maxLength={field === 'name' ? 120 : field === 'country' ? 80 : field === 'region' ? 100 : 30} onChange={e => setZone({ ...zone, [field]: e.target.value })} /></label>)}</div><div className="form-row">{['isActive', 'requiresPhone', 'requiresRegion', 'requiresPostalCode'].map(field => <label key={field}><input type="checkbox" checked={zone[field]} onChange={e => setZone({ ...zone, [field]: e.target.checked })} />{field.replace(/([A-Z])/g, ' $1')}</label>)}</div><button className="btn btn-primary" disabled={pending}>Save zone</button></form>
    </div></section>
    <section className="card"><div className="card-header"><h3>Delivery methods</h3><button className="btn btn-outline-primary" onClick={() => setMethod(newMethod())}>New method</button></div><div className="card-body">
      <label>Edit delivery method<select className="form-control" value={method.id || ''} onChange={e => setMethod(resource.data.methods.find(m => m.id === Number(e.target.value)) || newMethod())}><option value="">New method</option>{resource.data?.methods.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}</select></label>
      <form onSubmit={e => save(e, 'method')} className="add-product-form"><div className="form-row"><label>Zone<select className="form-control" required value={method.shippingZoneId || ''} onChange={e => setMethod({ ...method, shippingZoneId: e.target.value })}><option value="">Choose zone</option>{resource.data?.zones.map(z => <option key={z.id} value={z.id}>{z.name}</option>)}</select></label>{['name', 'code', 'currency'].map(field => <label key={field}>{field}<input className="form-control" required value={method[field]} maxLength={field === 'currency' ? 3 : field === 'code' ? 50 : 120} pattern={field === 'code' ? '[A-Z0-9_-]+' : undefined} onChange={e => setMethod({ ...method, [field]: e.target.value })} /></label>)}</div><div className="form-row">{numeric.map(field => <label key={field}>{field.replace(/([A-Z])/g, ' $1')}<input className="form-control" type="number" min="0" step={field.includes('Days') || field === 'displayOrder' ? 1 : 0.01} value={method[field] ?? ''} onChange={e => setMethod({ ...method, [field]: e.target.value === '' ? null : Number(e.target.value) })} /></label>)}</div><div className="form-row">{['isActive', 'isPickup', 'isDevelopmentValue'].map(field => <label key={field}><input type="checkbox" checked={method[field]} onChange={e => setMethod({ ...method, [field]: e.target.checked })} />{field.replace(/([A-Z])/g, ' $1')}</label>)}</div><label>Pickup location<input className="form-control" required={method.isPickup} value={method.pickupLocation || ''} maxLength="500" onChange={e => setMethod({ ...method, pickupLocation: e.target.value })} /></label><button className="btn btn-primary" disabled={pending}>Save delivery method</button></form>
    </div></section>
  </div>;
}
