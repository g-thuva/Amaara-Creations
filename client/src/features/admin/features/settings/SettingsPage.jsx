import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useSettings } from './useSettings.js';
import { SettingsList } from './SettingsList.jsx';
import { SettingForm } from './SettingForm.jsx';
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
import { MutationFeedback } from '@/features/admin/shared/components/MutationFeedback.jsx';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { PageHeader, FilterBar, Card } from '@/features/admin/shared/components/AdminUi.jsx';
export default function SettingsPage() {
  const feature = useSettings();
  const [group, setGroup] = useState('');
  const [search, setSearch] = useState('');
  const stale = feature.resource.loading || Boolean(feature.resource.error);
  const items = feature.items.filter(item => (!group || item.group === group) && (item.label + ' ' + item.description).toLowerCase().includes(search.toLowerCase()));
  return <div className="admin-page">
    <PageHeader title="Store settings" description="Manage the store information and content used by the live storefront." />
    <Card title="Business configuration"><p>Shipping, promotions and content have their own management pages.</p><div className="action-buttons"><Link className="btn btn-outline-primary" to="/admin/shipping">Shipping</Link><Link className="btn btn-outline-primary" to="/admin/promotions">Promotions</Link><Link className="btn btn-outline-primary" to="/admin/content">Content</Link></div></Card>
    <FilterBar><label>Search settings<input type="search" className="form-control" value={search} onChange={e => setSearch(e.target.value)} /></label><label>Settings group<select className="form-control" value={group} onChange={e => setGroup(e.target.value)}><option value="">All groups</option>{[...new Set(feature.items.map(item => item.group))].map(value => <option key={value}>{value}</option>)}</select></label></FilterBar>
    <MutationFeedback {...feature.mutation} />
    <ResourceState resource={feature.resource}><SettingsList items={items} open={feature.open} disabled={stale || feature.mutation.pending} />{feature.legacy.length > 0 && <p className="admin-helper">{feature.legacy.length} existing settings outside this supported catalogue are retained. They are not editable here because their storefront behavior is not defined.</p>}</ResourceState>
    {feature.editor && <AdminFormDialog title={'Edit ' + feature.form.label} description={feature.form.group + ' settings'} onClose={feature.close} pending={feature.mutation.pending} error={feature.mutation.error} dirty={JSON.stringify(feature.form) !== feature.editor.initial} submitDisabled={stale} submitLabel="Save setting" width="compact">{formId => <SettingForm {...feature} formId={formId} pending={feature.mutation.pending} />}</AdminFormDialog>}
  </div>;
}
