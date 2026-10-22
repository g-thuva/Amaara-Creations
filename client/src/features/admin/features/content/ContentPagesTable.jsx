import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { StatusBadge } from '@/features/admin/shared/components/StatusBadge.jsx';
import { formatDate } from '@/features/admin/shared/utils/format.js';
import AppIcon from '@/shared/components/ui/Icon.jsx';
export function ContentPagesTable({ pages, edit }) {
  return <DataTable caption="Content pages" rows={pages} empty="No content pages yet" emptyDescription="Create a page below and choose when to publish it." columns={[
    { key: 'title', label: 'Title' }, { key: 'slug', label: 'Slug' },
    { key: 'status', label: 'Status', render: page => <StatusBadge status={page.isPublished ? 'Published' : 'Draft'} /> },
    { key: 'updatedAt', label: 'Updated', render: page => formatDate(page.updatedAt) },
    { key: 'actions', label: 'Actions', render: page => <button className="btn-edit" aria-label={`Edit ${page.title}`} onClick={() => edit(page)}><AppIcon name="edit" /></button> },
  ]} />;
}
