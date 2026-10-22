import AppIcon from '@/shared/components/ui/Icon.jsx';
import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
export function SettingsList({ items, setForm }) {
  return <DataTable caption="Site settings" rows={items} empty="No settings yet" emptyDescription="Configured store settings will appear here." columns={[
    { key: 'key', label: 'Key' }, { key: 'value', label: 'Value', truncate: true }, { key: 'valueType', label: 'Type' },
    { key: 'isPublic', label: 'Public', render: item => item.isPublic ? 'Yes' : 'No' },
    { key: 'actions', label: 'Actions', render: item => <button className="btn-edit" aria-label={`Edit ${item.key}`} onClick={() => setForm(item)}><AppIcon name="edit" /></button> },
  ]} />;
}
