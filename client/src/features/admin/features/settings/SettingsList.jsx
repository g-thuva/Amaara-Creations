import { DataTable, Button, StatusChip } from '@/features/admin/shared/components/AdminUi.jsx';
export function SettingsList({ items, open, disabled }) {
  return <DataTable caption="Supported store settings" rows={items} empty="No settings match" emptyDescription="Try another search or settings group." columns={[
    { key: 'label', label: 'Setting' }, { key: 'group', label: 'Group' },
    { key: 'value', label: 'Value', truncate: true, render: item => item.configured ? item.value : 'Using store default' },
    { key: 'isPublic', label: 'Visibility', render: item => <StatusChip status={!item.configured ? 'Default' : item.isPublic ? 'Active' : 'Disabled'} /> },
    { key: 'description', label: 'Used by' },
    { key: 'actions', label: 'Actions', render: item => <Button variant="secondary" size="small" disabled={disabled} aria-label={(item.configured ? 'Edit ' : 'Configure ') + item.label} onClick={() => open(item)}>{item.configured ? 'Edit' : 'Configure'}</Button> },
  ]} />;
}
