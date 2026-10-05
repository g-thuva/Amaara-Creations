import { useState } from 'react';
import { addressApi } from '../services/addressApi';
import { useStore } from '../contexts/StoreContext';
import { useResource } from '../hooks/useResource';
import { Button, ConfirmDialog, Dialog, EmptyState, ErrorState, PageHeader, Skeleton } from '../components/storefront/UI';
const fields = [['label', 'Label', 80, true, 'off'], ['recipientName', 'Recipient name', 120, true, 'name'], ['phone', 'Phone', 40, false, 'tel'], ['addressLine1', 'Address line 1', 200, true, 'address-line1'], ['addressLine2', 'Address line 2', 200, false, 'address-line2'], ['city', 'City', 100, true, 'address-level2'], ['districtOrProvince', 'District or province', 100, false, 'address-level1'], ['postalCode', 'Postal code', 30, false, 'postal-code'], ['country', 'Country', 80, true, 'country-name']];
export default function Addresses() {
  const data = useResource(addressApi.getAddresses, 'addresses');
  const { mutate, pending } = useStore();
  const [editing, setEditing] = useState(null);
  const [deleting, setDeleting] = useState(null);
  const save = async e => {
    e.preventDefault(); const form = new FormData(e.currentTarget);
    const payload = Object.fromEntries(fields.map(([key]) => [key, form.get(key)])); payload.isDefault = form.get('isDefault') === 'on';
    if (await mutate(() => editing.id ? addressApi.updateAddress(editing.id, payload) : addressApi.createAddress(payload), data.reload, 'Address saved.')) setEditing(null);
  };
  return <><PageHeader title="Address book">Your saved delivery details.</PageHeader><Button onClick={() => setEditing({ label: 'Home', country: 'Sri Lanka' })}>Add an address</Button>{data.loading ? <Skeleton count={2}/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : !data.data?.length ? <EmptyState title="No saved addresses yet" to={null}>Add an address to keep your delivery details together.</EmptyState> : <div className="s-address-grid">{data.data.map(address => <article className="s-panel" key={address.id}><div className="s-row"><h2>{address.label}</h2>{address.isDefault && <span className="s-badge">Default</span>}</div><address><strong>{address.recipientName}</strong><br/>{address.addressLine1}<br/>{address.addressLine2 && <>{address.addressLine2}<br/></>}{address.city}, {address.districtOrProvince} {address.postalCode}<br/>{address.country}{address.phone && <><br/>{address.phone}</>}</address><div className="s-actions"><Button variant="text" disabled={pending} onClick={() => setEditing(address)}>Edit</Button><Button variant="text" disabled={pending} onClick={() => setDeleting(address)}>Delete</Button>{!address.isDefault && <Button variant="text" disabled={pending} onClick={() => mutate(() => addressApi.setDefault(address.id), data.reload, 'Default address updated.')}>Set default</Button>}</div></article>)}</div>}
    <Dialog open={!!editing} onClose={pending ? () => {} : () => setEditing(null)} title={editing?.id ? 'Edit address' : 'Add an address'}><form className="s-form" onSubmit={save}><fieldset disabled={pending}>{fields.map(([key, label, max, required, autocomplete]) => <label key={key}>{label}{required ? ' *' : ''}<input name={key} type={key === 'phone' ? 'tel' : 'text'} autoComplete={autocomplete} required={required} maxLength={max} defaultValue={editing?.[key] || ''}/></label>)}<label className="s-checkbox"><input type="checkbox" name="isDefault" defaultChecked={editing?.isDefault}/> Make this my default address</label><Button type="submit" disabled={pending}>{pending ? 'Saving…' : 'Save address'}</Button></fieldset></form></Dialog>
    <ConfirmDialog open={!!deleting} title="Delete this address?" pending={pending} onClose={() => setDeleting(null)} onConfirm={async () => { if (await mutate(() => addressApi.deleteAddress(deleting.id), data.reload, 'Address deleted.')) setDeleting(null); }}>{deleting?.label} will be removed from your address book.</ConfirmDialog>
  </>;
}
