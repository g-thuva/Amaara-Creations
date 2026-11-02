import { useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { useCategories } from "@/features/admin/features/categories/useCategories.js";
import { CategoriesTable } from "@/features/admin/features/categories/CategoriesTable.jsx";
import { CategoryForm } from "@/features/admin/features/categories/CategoryForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function CategoriesPage() {
  const feature = useCategories();
  const [open, setOpen] = useState(false);
  const close = () => { feature.reset(); setOpen(false); };
  const create = () => { feature.reset(); setOpen(true); };
  const edit = item => { feature.mutation.reset(); feature.edit(item); setOpen(true); };
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h1>Categories</h1>
            <p>{feature.items.length} categories</p>
          </div>
          <button className="btn-add-product" type="button" onClick={create}><AppIcon name="plus" /> New Category</button>
        </div>
      </div>
      <MutationFeedback {...feature.mutation} />
      <ResourceState resource={feature.resource}>
        <CategoriesTable {...feature} edit={edit} />
      </ResourceState>
      {open && <AdminFormDialog title={feature.editing ? 'Edit Category' : 'New Category'} description="Organise your store catalogue."
        width="compact" onClose={close} dirty={feature.dirty} pending={feature.mutation.pending} submitDisabled={false} error={feature.mutation.error}
        submitLabel={feature.editing ? 'Save Changes' : 'Create Category'}>
        {formId => <CategoryForm {...feature} formId={formId} pending={feature.mutation.pending || false}
          save={async event => { if (await feature.save(event)) setOpen(false); }} />}
      </AdminFormDialog>}
    </div>
  );
}
