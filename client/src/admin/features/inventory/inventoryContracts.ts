export interface StockBalance {
  productId: number;
  variantId: number | null;
  product: string;
  variant: string;
  sku: string | null;
  available: number;
  reserved: number;
  onHand: number;
  lowStockThreshold: number;
}
export interface InventoryMovement {
  id: number;
  productId: number;
  productVariantId: number | null;
  previousQuantity: number;
  quantityDelta: number;
  newQuantity: number;
  movementType: string;
  orderId: number | null;
  reason: string | null;
  performedByUserId: string | null;
  createdAt: string;
}
export interface InventoryAdjustment {
  variantId: number | null;
  expectedAvailable: number;
  quantityDelta: number;
  reason: string;
  lowStockThreshold: number;
}
export interface BalancePage {
  items: StockBalance[];
  totalCount: number;
  page: number;
  pageSize: number;
}
// The existing movements endpoint returns only items/totalCount, with a fixed size of 50.
export interface MovementPage {
  items: InventoryMovement[];
  totalCount: number;
}
