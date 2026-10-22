import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { StatusBadge } from '@/features/admin/shared/components/StatusBadge.jsx';
import { ConfirmAction } from '@/features/admin/shared/components/ConfirmAction.jsx';
import AppIcon from '@/shared/components/ui/Icon.jsx';
export function CollectionsTable({ items, edit, toggle }) {
  return <DataTable caption="Collections" rows={items} empty="No collections yet" emptyDescription="Create a collection below to group related products." columns={[
    { key: 'name', label: 'Name' }, { key: 'slug', label: 'Slug' }, { key: 'productCount', label: 'Products' },
    { key: 'status', label: 'Status', render: item => <StatusBadge status={item.isActive ? 'Active' : 'Archived'} /> },
    { key: 'actions', label: 'Actions', render: item => <div className="action-buttons"><button className="btn-edit" aria-label={`Edit ${item.name}`} onClick={() => edit(item)}><AppIcon name="edit" /></button><ConfirmAction onConfirm={() => toggle(item)} title={item.isActive ? 'Archive collection' : 'Reactivate collection'} detail={`Review the availability change for ${item.name}.`} destructive={item.isActive}><button className="btn-delete" aria-label={`${item.isActive ? 'Archive' : 'Reactivate'} ${item.name}`}><AppIcon name={item.isActive ? 'archive' : 'refresh'} /></button></ConfirmAction></div> },
  ]} />;
}
