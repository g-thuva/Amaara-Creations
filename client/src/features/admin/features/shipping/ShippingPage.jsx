import { useShippingConfiguration } from "@/features/admin/features/shipping/useShippingConfiguration.js";
import { ShippingZoneForm } from "@/features/admin/features/shipping/ShippingZoneForm.jsx";
import { ShippingMethodForm } from "@/features/admin/features/shipping/ShippingMethodForm.jsx";
export default function ShippingPage() {
  const shipping = useShippingConfiguration();
  const { resource, message } = shipping;
  return (
    <div className="admin-page">
      <div className="page-header">
        <h2>Shipping zones and methods</h2>
      </div>
      <p>
        Enter business-approved rates, regions, delivery estimates and pickup
        locations. Development values are excluded from checkout unless
        explicitly enabled in Development.
      </p>
      {resource.loading && <p role="status">Loading shipping configuration…</p>}
      {resource.error && (
        <p role="alert">
          Shipping configuration could not be loaded.{" "}
          <button onClick={resource.reload}>Try again</button>
        </p>
      )}
      {message && (
        <p role={message.success ? "status" : "alert"}>{message.text}</p>
      )}
      <ShippingZoneForm {...shipping} />
      <ShippingMethodForm {...shipping} />
    </div>
  );
}
