import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { settingsApi } from './settingsApi.js';
export function useSettings() {
  const [editor, setEditor] = useState(null);
  const resource = useResource(async signal => {
    const [saved, definitions] = await Promise.all([settingsApi.getSettings(signal), settingsApi.getCatalogue(signal)]);
    return { saved, definitions };
  }, 'store-settings');
  const mutation = useAdminMutation(resource.reload);
  const saved = resource.data?.saved || [];
  const definitions = resource.data?.definitions || [];
  const items = definitions.map(definition => {
    const current = saved.find(item => item.key === definition.key);
    return { ...current, ...definition, value: current?.value || '', isPublic: current?.isPublic ?? true, valueType: 'text', configured: Boolean(current), updatedAt: current?.updatedAt };
  });
  const legacy = saved.filter(item => !definitions.some(definition => definition.key === item.key));
  const open = value => { mutation.reset(); setEditor({ value: { ...value }, initial: JSON.stringify(value) }); };
  const setForm = value => setEditor(current => ({ ...current, value }));
  const save = async event => {
    event.preventDefault();
    if (resource.loading || resource.error) return;
    const form = editor.value;
    if (await mutation.run(() => settingsApi.saveSetting({ key: form.key, value: form.value.trim(), isPublic: form.isPublic, valueType: 'text', description: form.description, expectedUpdatedAt: form.updatedAt || null }), 'Store setting saved. Storefront pages refresh settings when reopened or focused.')) setEditor(null);
  };
  return { items, legacy, editor, form: editor?.value, setForm, open, close: () => setEditor(null), save, resource, mutation };
}
