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
      {message && (
        <p role={message.success ? "status" : "alert"}>{message.text}</p>
      )}
      <ShippingZoneForm {...shipping} />
      <ShippingMethodForm {...shipping} />
      </ResourceState>
    </div>
  );
}
