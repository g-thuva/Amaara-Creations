import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { shippingApi } from './shippingApi.js';
import { newZone, newMethod, validateMethod } from './shippingForm.js';

export function useShippingConfiguration() {
  const resource = useResource(shippingApi.getConfiguration, 'shipping', true, { keepPreviousData: true });
  const mutation = useAdminMutation(resource.reload);
  const [editor, setEditor] = useState(null);
  const [intent, setIntent] = useState(null);
  const [validation, setValidation] = useState(null);
  const open = (type, value) => {
    const draft = { ...(value || (type === 'zone' ? newZone() : { ...newMethod(), shippingZoneId: resource.data?.zones.length === 1 ? resource.data.zones[0].id : '' })) };
    mutation.reset(); setValidation(null);
    setEditor({ type, value: draft, initial: JSON.stringify(draft) });
  };
  const setValue = value => setEditor(current => ({ ...current, value }));
  const persist = (type, value) => type === 'zone' ? shippingApi.saveZone(value) : shippingApi.saveMethod({ ...value, shippingZoneId: Number(value.shippingZoneId), basePrice: Number(value.basePrice) });
  const save = async event => {
    event.preventDefault();
    const error = editor.type === 'method' ? validateMethod(editor.value) : null;
    setValidation(error);
    if (error) return;
    const value = editor.value;
    const message = editor.type === 'zone' ? 'Delivery zone saved.' : !value.isActive ? 'Delivery method saved. It is inactive and will not appear at checkout.' : value.isDevelopmentValue ? 'Delivery method saved as a test rate. It is excluded from regular checkout.' : 'Delivery method saved. It is available at checkout for matching addresses and currency.';
    if (await mutation.run(() => persist(editor.type, value), message)) setEditor(null);
  };
  const confirmToggle = async () => {
    if (await mutation.run(() => persist(intent.type, { ...intent.value, isActive: !intent.value.isActive }), 'Availability updated.')) setIntent(null);
  };
  return { resource, mutation, editor, setEditor, setValue, open, save, validation, intent, setIntent, confirmToggle };
}
