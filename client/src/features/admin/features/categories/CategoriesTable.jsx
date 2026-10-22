import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { StatusBadge } from '@/features/admin/shared/components/StatusBadge.jsx';
import { ConfirmAction } from '@/features/admin/shared/components/ConfirmAction.jsx';
import AppIcon from '@/shared/components/ui/Icon.jsx';
export function CategoriesTable({ items, edit, toggle }) {
  return <DataTable caption="Categories" rows={items} empty="No categories yet" emptyDescription="Create a category below to organise your products." columns={[
    { key: 'name', label: 'Name' }, { key: 'slug', label: 'Slug' },
    { key: 'parent', label: 'Parent', render: category => items.find(item => item.id === category.parentCategoryId)?.name || '—' },
    { key: 'sortOrder', label: 'Sort' },
    { key: 'status', label: 'Status', render: category => <StatusBadge status={category.isActive ? 'Active' : 'Archived'} /> },
    { key: 'actions', label: 'Actions', render: category => <div className="action-buttons"><button className="btn-edit" aria-label={`Edit ${category.name}`} onClick={() => edit(category)}><AppIcon name="edit" /></button><ConfirmAction onConfirm={() => toggle(category)} title={category.isActive ? 'Archive category' : 'Reactivate category'} detail={`Review the availability change for ${category.name}.`} destructive={category.isActive}><button className="btn-delete" aria-label={`${category.isActive ? 'Archive' : 'Reactivate'} ${category.name}`}><AppIcon name={category.isActive ? 'archive' : 'refresh'} /></button></ConfirmAction></div> },
  ]} />;
}
