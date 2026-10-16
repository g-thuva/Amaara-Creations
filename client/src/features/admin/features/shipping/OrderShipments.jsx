import { formatDateTime } from "@/shared/utils/formatDate.ts";
import { useState } from "react";
import { shippingApi } from "@/features/admin/features/shipping/shippingApi.js";
import { ShipmentForm } from "@/features/admin/features/shipping/ShipmentForm.jsx";
export function OrderShipments({ order, changed, canManage, stale = false }) {
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState(null);
  const updateShipment = async (event) => {
    event.preventDefault();
    if (pending) return;
    const form = Object.fromEntries(new FormData(event.currentTarget));
    setPending(true);
    setNotice(null);
    try {
      await shippingApi.updateShipment(order.id, {
        ...form,
        courier: form.courier || null,
        trackingNumber: form.trackingNumber || null,
        trackingUrl: form.trackingUrl || null,
      });
      changed();
      setNotice({ success: true, text: "Fulfilment updated." });
    } catch (e) {
      setNotice({
        text: e.response?.data?.title || "Fulfilment could not be updated.",
      });
    } finally {
      setPending(false);
    }
  };
  const shipment = order.shipments?.[0];
  const shipmentState = shipment?.status || "Pending";
  const nextShipment = order.isPickup
    ? { Pending: "ReadyForPickup", ReadyForPickup: "Delivered" }[shipmentState]
    : { Pending: "Packed", Packed: "Shipped", Shipped: "Delivered" }[
        shipmentState
      ];
  const canUpdate =
    canManage &&
    (nextShipment || shipment) &&
    ["Confirmed", "Processing", "Shipped", "Completed", "Delivered"].includes(
      order.status,
    );
  return (
    <>
      {order.shipments?.map((s) => (
        <section className="card card-body" key={s.id}>
          <h2>Shipment details</h2>
          <p>
            {s.status} · {s.courier} · Tracking{" "}
            {s.trackingNumber || "Not assigned"}
          </p>
          <p>
            Dispatched:{" "}
            {s.shippedAt ? formatDateTime(s.shippedAt) : "Not dispatched"} ·
            Delivered:{" "}
            {s.deliveredAt ? formatDateTime(s.deliveredAt) : "Not delivered"}
          </p>
        </section>
      ))}
      {notice && (
        <p role={notice.success ? "status" : "alert"}>{notice.text}</p>
      )}
      {canUpdate && (
        <ShipmentForm
          key={shipmentState}
          shipment={shipment}
          shipmentState={shipmentState}
          nextShipment={nextShipment}
          pending={pending || stale}
          onSubmit={updateShipment}
        />
      )}
    </>
  );
}
