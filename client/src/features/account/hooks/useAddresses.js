import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { addressApi } from '@/features/account/addressApi.js';
import { addressFields } from '@/features/account/addressForm.js';

export function useAddresses() {
  const data = useResource(addressApi.getAddresses, 'addresses');
  const { mutate, pending } = useAuthenticatedMutation();
  const [editing, setEditing] = useState(null);
  const [deleting, setDeleting] = useState(null);

  const save = async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = Object.fromEntries(addressFields.map(([key]) => [key, form.get(key)]));
    payload.isDefault = form.get('isDefault') === 'on';
    const action = editing.id ? () => addressApi.updateAddress(editing.id, payload) : () => addressApi.createAddress(payload);
    if (await mutate(action, data.reload, 'Address saved.')) setEditing(null);
  };


  const remove = async () => { if (await mutate(() => addressApi.deleteAddress(deleting.id), data.reload, 'Address deleted.')) setDeleting(null); };
  const makeDefault = id => mutate(() => addressApi.setDefault(id), data.reload, 'Default address updated.');
  return { data, pending, editing, setEditing, deleting, setDeleting, save, remove, makeDefault };
}
