import { useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { useShippingConfiguration } from './useShippingConfiguration.js';
import { ShippingZoneForm } from './ShippingZoneForm.jsx';
import { ShippingMethodForm } from './ShippingMethodForm.jsx';
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { MutationFeedback } from '@/features/admin/shared/components/MutationFeedback.jsx';
import { PageHeader, Card, DataTable, FilterBar, StatusChip, Button, ConfirmDialog } from '@/features/admin/shared/components/AdminUi.jsx';
import { formatIsoCurrency } from '@/features/admin/shared/utils/format.js';

export default function ShippingPage() {
  const shipping = useShippingConfiguration();
  const { resource, mutation, editor, open, intent, setIntent } = shipping;
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const zones = resource.data?.zones || [];
  const methods = resource.data?.methods || [];
  const stale = resource.loading || Boolean(resource.error);
  const matches = item => [item.name, item.code, item.country, item.region].filter(Boolean).join(' ').toLowerCase().includes(search.toLowerCase()) && (!status || item.isActive === (status === 'active'));
  const actions = (type, row) => <div className="action-buttons"><Button variant="secondary" size="small" disabled={mutation.pending || stale} onClick={() => open(type, row)}>Edit {type === 'zone' ? 'zone' : 'method'}</Button><Button variant="ghost" size="small" disabled={mutation.pending || stale} onClick={() => { mutation.reset(); setIntent({ type, value: row }); }}>{row.isActive ? 'Disable' : 'Enable'}</Button></div>;
  return (
    <div className="admin-page admin-shipping">
      <PageHeader
        title="Shipping zones and methods"
        description="Manage delivery coverage, approved rates and customer pickup."
      />
      <p>
        Development rates remain excluded from production checkout. Delivery
        options must match the customer’s address and currency.
      </p>
      <MutationFeedback {...mutation} />
      <ResourceState
        resource={resource}
        errorTitle="Shipping configuration could not be loaded."
      >
        <FilterBar>
          <label>
            Search zones or methods
            <input
              type="search"
              className="form-control"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </label>
          <label>
            Availability
            <select
              className="form-control"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              <option value="">All</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </label>
        </FilterBar>
        <Card
          title="Delivery zones"
          actions={
            <Button
              onClick={() => open("zone")}
              disabled={mutation.pending || stale}
            >
              <AppIcon name="plus" />
              New zone
            </Button>
          }
        >
          <DataTable
            caption="Delivery zone coverage"
            rows={zones.filter(matches)}
            empty="No delivery zones found"
            emptyDescription={
              zones.length
                ? "Try another search or availability filter."
                : "Create a zone, then add a delivery method with your approved rate."
            }
            columns={[
              { key: "name", label: "Zone" },
              { key: "country", label: "Country" },
              { key: "region", label: "Region" },
              { key: "postalCodePrefix", label: "Postal prefix" },
              {
                key: "isActive",
                label: "Availability",
                render: (row) => (
                  <StatusChip status={row.isActive ? "Active" : "Disabled"} />
                ),
              },
              {
                key: "methods",
                label: "Delivery methods",
                render: (row) =>
                  methods
                    .filter((method) => method.shippingZoneId === row.id)
                    .map((method) => method.name)
                    .join(", ") || "None configured",
              },
              {
                key: "actions",
                label: "Actions",
                render: (row) => actions("zone", row),
              },
            ]}
          />
        </Card>
        <Card
          title="Delivery methods"
          actions={
            <Button
              onClick={() => open("method")}
              disabled={!zones.length || mutation.pending || stale}
            >
              <AppIcon name="plus" />
              New method
            </Button>
          }
        >
          <DataTable
            caption="Delivery method rates"
            rows={methods.filter(matches)}
            empty="No delivery methods found"
            emptyDescription={
              methods.length
                ? "Try another search or availability filter."
                : zones.length
                  ? "Add a method using your business-approved delivery rate."
                  : "Create a delivery zone before adding a method."
            }
            columns={[
              { key: "name", label: "Method" },
              { key: "code", label: "Code" },
              {
                key: "zone",
                label: "Zone",
                render: (row) =>
                  zones.find((zone) => zone.id === row.shippingZoneId)?.name ||
                  "Unavailable",
              },
              {
                key: "price",
                label: "Base price",
                render: (row) => formatIsoCurrency(row.basePrice, row.currency),
              },
              {
                key: "isActive",
                label: "Availability",
                render: (row) => (
                  <StatusChip status={row.isActive ? "Active" : "Disabled"} />
                ),
              },
              {
                key: "type",
                label: "Type",
                render: (row) =>
                  row.isDevelopmentValue
                    ? "Test rate"
                    : row.isPickup
                      ? "Customer pickup"
                      : "Delivery",
              },
              {
                key: "actions",
                label: "Actions",
                render: (row) => actions("method", row),
              },
            ]}
          />
        </Card>
      </ResourceState>
      {editor && (
        <AdminFormDialog
          title={
            (editor.value.id ? "Edit" : "New") + " delivery " + editor.type
          }
          description={
            editor.type === "zone"
              ? "Define delivery coverage and the address details required at checkout."
              : "Configure an approved rate, delivery estimates and availability."
          }
          onClose={() => shipping.setEditor(null)}
          dirty={JSON.stringify(editor.value) !== editor.initial}
          pending={mutation.pending}
          error={shipping.validation || mutation.error}
          submitLabel={
            editor.type === "zone" ? "Save zone" : "Save delivery method"
          }
        >
          {(formId) =>
            editor.type === "zone" ? (
              <ShippingZoneForm
                {...shipping}
                value={editor.value}
                formId={formId}
                pending={mutation.pending}
              />
            ) : (
              <ShippingMethodForm
                {...shipping}
                value={editor.value}
                formId={formId}
                pending={mutation.pending}
              />
            )
          }
        </AdminFormDialog>
      )}
      <ConfirmDialog
        open={Boolean(intent)}
        onClose={() => setIntent(null)}
        onConfirm={shipping.confirmToggle}
        pending={mutation.pending}
        title={
          (intent?.value.isActive ? "Disable" : "Enable") +
          " delivery " +
          (intent?.type || "option") +
          "?"
        }
      >
        {mutation.error && <p role="alert">{mutation.error}</p>}
        <p>{intent?.value.name}</p>
        <p>
          {intent?.value.isActive
            ? "This option will no longer be available for new checkout selections. Existing order records are retained."
            : "This option can appear at checkout when its zone, address and currency requirements match. Test rates remain excluded from production."}
        </p>
      </ConfirmDialog>
    </div>
  );
}
