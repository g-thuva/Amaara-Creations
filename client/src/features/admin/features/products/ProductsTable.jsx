import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { StatusBadge } from '@/features/admin/shared/components/StatusBadge.jsx';
import { ConfirmAction } from '@/features/admin/shared/components/ConfirmAction.jsx';
import { resolveMediaUrl } from '@/shared/api/requestConfig.js';
import { formatIsoCurrency as money } from '@/features/admin/shared/utils/format.js';
import AppIcon from '@/shared/components/ui/Icon.jsx';
export function ProductsTable({ products, isLoading, startEdit, openProductTools, toggleArchive, pending }) {
  return <DataTable caption="Product catalog" rows={products} loading={isLoading} empty="No products found." emptyDescription="Create a product below, or change your search and status filters." columns={[
    { key: 'image', label: 'Image', render: product => <img className="product-thumbnail" src={resolveMediaUrl(product.imageUrl)} alt={product.name} /> },
    { key: 'name', label: 'Name', render: product => <div className="product-name-cell"><strong>{product.name}</strong><small>{product.baseSku || product.slug}</small></div> },
    { key: 'category', label: 'Category', render: product => product.categoryName || product.category },
    { key: 'price', label: 'Price', render: product => money(product.basePrice, product.currency) },
    { key: 'stock', label: 'Stock' },
    { key: 'status', label: 'Status', render: product => <StatusBadge status={product.isActive ? 'Active' : 'Archived'} /> },
    { key: 'actions', label: 'Actions', render: product => <div className="action-buttons"><button className="btn-edit" disabled={pending} title="Edit" onClick={() => startEdit(product)}><AppIcon name="edit" /></button><button className="btn-edit" disabled={pending} title="Media and variants" onClick={() => openProductTools(product)}><AppIcon name="layers" /></button><ConfirmAction pending={pending} onConfirm={() => toggleArchive(product)} title={`${product.isActive ? 'Archive' : 'Reactivate'} product`} detail={`${product.isActive ? 'Archive' : 'Reactivate'} ${product.name}? Existing order snapshots will be preserved.`} destructive={product.isActive}><button className="btn-delete" disabled={pending} title={product.isActive ? 'Archive' : 'Reactivate'}><AppIcon name={product.isActive ? 'archive' : 'refresh'} /></button></ConfirmAction></div> },
  ]} />;
}
