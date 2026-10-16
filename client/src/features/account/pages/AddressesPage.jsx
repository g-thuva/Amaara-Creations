import AddressForm from '@/features/account/components/AddressForm.jsx';
import { useAddresses } from '@/features/account/hooks/useAddresses.js';
import { Button } from '@/shared/components/ui/Button.jsx';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog.jsx';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';

export default function AddressesPage() {
  const { data, pending, editing, setEditing, deleting, setDeleting, save, remove, makeDefault } = useAddresses();
  return (
    <>
      <PageHeader title="Address book">Manage the delivery details stored with your account.</PageHeader>
      <Button onClick={() => setEditing({ label: 'Home', country: 'Sri Lanka' })}><Icon name="plus" />Add an address</Button>
      {data.loading ? <Skeleton count={2} /> : data.error ? <ErrorState error={data.error} retry={data.reload} /> : !data.data?.length ? (
        <EmptyState title="No saved addresses yet" to={null}><p>Add an address to keep your delivery details together.</p></EmptyState>
      ) : (
        <div className="s-address-grid">
          {data.data.map((address) => (
            <article className="s-panel" key={address.id}>
              <div className="s-row"><h2>{address.label}</h2>{address.isDefault && <span className="s-badge"><Icon name="check" />Default</span>}</div>
              <address><strong>{address.recipientName}</strong><br />{address.addressLine1}<br />{address.addressLine2 && <>{address.addressLine2}<br /></>}{address.city}, {address.districtOrProvince} {address.postalCode}<br />{address.country}{address.phone && <><br />{address.phone}</>}</address>
              <div className="s-actions">
                <Button variant="text" disabled={pending} onClick={() => setEditing(address)}><Icon name="edit" />Edit</Button>
                <Button variant="text" disabled={pending} onClick={() => setDeleting(address)}><Icon name="trash" />Delete</Button>
                {!address.isDefault && <Button variant="text" disabled={pending} onClick={() => makeDefault(address.id)}>Set default</Button>}
              </div>
            </article>
          ))}
        </div>
      )}

      <Dialog open={!!editing} onClose={pending ? () => {} : () => setEditing(null)} title={editing?.id ? 'Edit address' : 'Add an address'}>
        <AddressForm editing={editing} pending={pending} save={save} />
      </Dialog>

      <ConfirmDialog open={!!deleting} title="Delete this address?" pending={pending} onClose={() => setDeleting(null)} onConfirm={remove}>
        {deleting?.label} will be removed from your address book.
      </ConfirmDialog>
    </>
  );
}
