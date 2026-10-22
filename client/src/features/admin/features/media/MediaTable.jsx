import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { resolveMediaUrl } from '@/shared/api/requestConfig.js';
export function MediaTable({ items }) {
  return <DataTable caption="Media library" rows={items} empty="No media assets yet" emptyDescription="Uploaded product images and content media will appear here." columns={[
    { key: 'preview', label: 'Preview', render: item => <img className="product-thumbnail" src={resolveMediaUrl(item.url)} alt={item.altText || item.originalFileName || 'Media asset'} /> },
    { key: 'file', label: 'File', render: item => item.originalFileName || item.storageKey }, { key: 'altText', label: 'Alt text' },
    { key: 'size', label: 'Size', render: item => Number.isFinite(item.fileSize) ? Math.round(item.fileSize / 1024) + ' KB' : 'Not provided' },
    { key: 'dimensions', label: 'Dimensions', render: item => item.width != null && item.height != null ? `${item.width} × ${item.height}` : 'Not provided' }, { key: 'storageProvider', label: 'Provider' },
  ]} />;
}
