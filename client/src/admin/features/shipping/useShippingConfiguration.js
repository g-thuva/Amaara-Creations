import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { shippingApi } from "./shippingApi.js";
import { newZone, newMethod } from "./shippingForm.js";
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
      if (type === "zone") await shippingApi.saveZone(zone);
      else
        await shippingApi.saveMethod({
          ...method,
          shippingZoneId: Number(method.shippingZoneId),
        });
      resource.reload();
      setMessage({ success: true, text: "Shipping configuration saved." });
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
