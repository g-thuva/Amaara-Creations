export interface ProductionAsset {
  id: number;
  assetType: string;
  originalFileName: string | null;
  downloadUrl: string;
}
export interface ProofRevision {
  id: number;
  revisionNumber: number;
  status: string;
  customerVisibleMessage: string | null;
  customerResponse: string | null;
  customerResponseAt: string | null;
  createdAt: string;
  downloadUrl: string;
}
export interface ProductionDesign {
  id: number;
  name: string;
  rowVersion: string;
  status: string;
  proofStatus: string;
  productionStatus: string;
  width: number;
  height: number;
  quantity: number;
  measurementUnit: string | null;
  currency: string;
  subtotal: number | null;
  pricingVersion: string | null;
  builderConfigurationVersion: number | null;
  shapeCode: string | null;
  shapeLabel: string | null;
  materialCode: string | null;
  materialLabel: string | null;
  finishCode: string | null;
  finishLabel: string | null;
  assets: ProductionAsset[];
  proofRevisions: ProofRevision[];
}
export interface ProductionJob {
  design: ProductionDesign;
  customer: { name: string | null; email: string | null };
  order: {
    orderNumber: string;
    configurationSnapshotJson: string | null;
  } | null;
  availableProductionTransitions: string[];
  canArchive: boolean;
  timeline: Array<{
    action: string;
    createdAt: string;
    actorUserId: string | null;
    newValuesJson: string | null;
  }>;
}
export interface ProductionTransition {
  status: string;
  note: string;
  rowVersion: string;
}
export interface ProductionFilters {
  search?: string;
  designStatus?: string;
  proofStatus?: string;
  productionStatus?: string;
  page?: number;
  pageSize?: number;
}
export interface ProductionQueue {
  items: Array<
    Pick<
      ProductionDesign,
      | "id"
      | "name"
      | "status"
      | "proofStatus"
      | "productionStatus"
      | "width"
      | "height"
      | "quantity"
      | "currency"
    > & {
      orderNumber: string | null;
      customerName: string | null;
      calculatedPrice: number;
    }
  >;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
