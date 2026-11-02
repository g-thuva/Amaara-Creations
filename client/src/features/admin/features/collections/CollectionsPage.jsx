import { useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { useCollections } from "@/features/admin/features/collections/useCollections.js";
import { CollectionsTable } from "@/features/admin/features/collections/CollectionsTable.jsx";
import { CollectionForm } from "@/features/admin/features/collections/CollectionForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function CollectionsPage() {
  const feature = useCollections();
  const [open, setOpen] = useState(false);
  const close = () => { feature.reset(); setOpen(false); };
  const create = () => { feature.reset(); setOpen(true); };
  const edit = item => { feature.mutation.reset(); feature.edit(item); setOpen(true); };
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h1>Collections</h1>
            <p>{feature.items.length} collections</p>
          </div>
          <button className="btn-add-product" type="button" onClick={create}><AppIcon name="plus" /> New Collection</button>
        </div>
      </div>
      <MutationFeedback {...feature.mutation} />
      <ResourceState resource={feature.resource}>
        <CollectionsTable {...feature} edit={edit} />
      </ResourceState>
      {open && <AdminFormDialog title={feature.editing ? 'Edit Collection' : 'New Collection'} description="Organise your store catalogue."
        width="compact" onClose={close} dirty={feature.dirty} pending={feature.mutation.pending} submitDisabled={feature.selectionPending || false} error={feature.mutation.error}
        submitLabel={feature.editing ? 'Save Changes' : 'Create Collection'}>
        {formId => <>
          {feature.editing && <ResourceState resource={feature.selection} />}
          <CollectionForm {...feature} formId={formId} pending={feature.mutation.pending || feature.selectionPending || false}
          save={async event => { if (await feature.save(event)) setOpen(false); }} />
        </>}
      </AdminFormDialog>}
    </div>
  );
}
