import { useShippingConfiguration } from "@/features/admin/features/shipping/useShippingConfiguration.js";
import { ShippingZoneForm } from "@/features/admin/features/shipping/ShippingZoneForm.jsx";
import { ShippingMethodForm } from "@/features/admin/features/shipping/ShippingMethodForm.jsx";
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
export default function ShippingPage() {
  const shipping = useShippingConfiguration();
  const { resource, message } = shipping;
  return (
    <div className="admin-page">
      <div className="page-header">
        <h1>Shipping zones and methods</h1>
      </div>
      <p>
        Enter business-approved rates, regions, delivery estimates and pickup
        locations. Development values are excluded from checkout unless
        explicitly enabled in Development.
      </p>
      <ResourceState resource={resource} errorTitle="Shipping configuration could not be loaded.">
      {!resource.data?.methods.length && (
        <p role="status">
          No delivery methods are configured. {resource.data?.zones.length
            ? 'Add a delivery method below using your approved rate.'
            : 'Create a delivery zone first, then add a delivery method using your approved rate.'}{' '}
          Checkout will show delivery options once an active method matches the customer’s address and currency.
        </p>
      )}
      {message && (
        <p role={message.success ? "status" : "alert"}>{message.text}</p>
      )}
      <ShippingZoneForm {...shipping} />
      <ShippingMethodForm {...shipping} />
      </ResourceState>
    </div>
  );
}
