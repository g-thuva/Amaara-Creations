import { SectionCard } from "@/features/admin/shared/components/SectionCard.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { SnapshotViewer } from "@/features/admin/shared/components/SnapshotViewer.jsx";
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
export function ProductionJobSummary({ data, design }) {
  return (
    <SectionCard title={design.name} inline>
      <p>
        {data.order?.orderNumber || "No order"} · {data.customer?.name}
      </p>
      <p>
        Design: <StatusChip status={design.status} /> · Proof: <StatusChip status={design.proofStatus} /> · Production:{" "}
        <StatusChip status={design.productionStatus} />
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
