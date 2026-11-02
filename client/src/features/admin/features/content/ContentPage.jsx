import { useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { useContent } from "@/features/admin/features/content/useContent.js";
import { ContentPagesTable } from "@/features/admin/features/content/ContentPagesTable.jsx";
import { ContentForm } from "@/features/admin/features/content/ContentForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function ContentPage() {
  const feature = useContent();
  const [open, setOpen] = useState(false);
  const close = () => { feature.reset(); setOpen(false); };
  const create = () => { feature.reset(); setOpen(true); };
  const edit = item => { feature.mutation.reset(); feature.edit(item); setOpen(true); };
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h1>Content</h1>
            <p>{feature.pages.length} pages</p>
          </div>
          <button className="btn-add-product" type="button" onClick={create}><AppIcon name="plus" /> New Page</button>
        </div>
      </div>
      <MutationFeedback {...feature.mutation} />
      <ResourceState resource={feature.resource}>
        <ContentPagesTable {...feature} edit={edit} />
      </ResourceState>
      {open && <AdminFormDialog title={feature.editing ? 'Edit Page' : 'New Page'} description="Manage page details and content sections."
        width="wide" onClose={close} dirty={feature.dirty} pending={feature.mutation.pending} submitDisabled={false} error={feature.mutation.error}
        submitLabel={feature.editing ? 'Save Changes' : 'Create Page'}>
        {formId => <ContentForm {...feature} formId={formId} pending={feature.mutation.pending || false}
          save={async event => { if (await feature.save(event)) setOpen(false); }} />}
      </AdminFormDialog>}
    </div>
  );
}
