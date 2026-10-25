import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { shippingApi } from "@/features/admin/features/shipping/shippingApi.js";
import { newZone, newMethod } from "@/features/admin/features/shipping/shippingForm.js";
export function useShippingConfiguration() {
  const resource = useResource(shippingApi.getConfiguration, "shipping");
  const [zone, setZone] = useState(newZone);
  const [method, setMethod] = useState(newMethod);
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState(null);
  const save = async (event, type) => {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setMessage(null);
    try {
      if (type === "zone") {
        const saved = await shippingApi.saveZone(zone);
        setZone(current => ({ ...current, id: saved.id }));
        setMethod(current => current.shippingZoneId ? current : { ...current, shippingZoneId: saved.id });
      } else {
        const saved = await shippingApi.saveMethod({
          ...method,
          shippingZoneId: Number(method.shippingZoneId),
        });
        setMethod(current => ({ ...current, id: saved.id }));
      }
      resource.reload();
      const text = type === "zone"
        ? "Delivery zone saved. Add a delivery method with your approved rate to offer it at checkout."
        : !method.isActive
          ? "Delivery method saved. It is inactive and will not appear at checkout."
          : method.isDevelopmentValue
            ? "Delivery method saved as a test rate. It is excluded from regular checkout."
            : "Delivery method saved. It is available at checkout for matching addresses and currency.";
      setMessage({ success: true, text });
    } catch (e) {
      setMessage({
        text:
          e.response?.data?.title ||
          "Shipping configuration could not be saved.",
      });
    } finally {
      setPending(false);
    }
  };
  return { resource, zone, setZone, method, setMethod, pending, message, save };
}
