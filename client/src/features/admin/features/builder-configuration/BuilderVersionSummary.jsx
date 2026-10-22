import { formatDateTime } from '@/features/admin/shared/utils/format.js';
import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
export function BuilderVersionSummary({ data }) {
  return <DataTable caption="Version history" rows={data.versions} empty="No builder versions yet" emptyDescription="Saved draft and published versions will appear here." columns={[{ key: 'versionNumber', label: 'Version' }, { key: 'status', label: 'Status' }, { key: 'publishedAt', label: 'Published', render: version => version.publishedAt ? formatDateTime(version.publishedAt) : '—' }]} />;
}
