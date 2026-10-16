import { SectionCard } from "../../shared/components/SectionCard.jsx";
import { checkoutMoney } from "../../../services/checkoutApi.js";
import { SnapshotViewer } from "../../shared/components/SnapshotViewer.jsx";
export function ProductionJobSummary({ data, design }) {
  return (
    <SectionCard title={design.name} inline>
      <p>
        {data.order?.orderNumber || "No order"} · {data.customer?.name}
      </p>
      <p>
        Design: {design.status} · Proof: {design.proofStatus} · Production:{" "}
        {design.productionStatus}
      </p>
      <p>
        {design.width} × {design.height} {design.measurementUnit} · Quantity{" "}
        {design.quantity} · {checkoutMoney(design.subtotal, design.currency)}
      </p>
      <p>
        Shape: {design.shapeLabel || design.shapeCode} · Material:{" "}
        {design.materialLabel || design.materialCode} · Finish:{" "}
        {design.finishLabel || design.finishCode}
      </p>
      <p>
        Configuration v{design.builderConfigurationVersion} · Pricing{" "}
        {design.pricingVersion}
      </p>
      <h3>Immutable order specification</h3>
      {data.order?.configurationSnapshotJson ? (
        <SnapshotViewer value={data.order.configurationSnapshotJson} />
      ) : (
        <p>No order snapshot is available.</p>
      )}
    </SectionCard>
  );
}
