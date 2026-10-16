export interface OrderItem {
  id: number;
  productName: string;
  productImageUrl: string;
  itemType: string;
  sku: string | null;
  quantity: number;
  price: number;
  subtotal: number;
  customDesignId: number | null;
  configurationSnapshotJson: string | null;
}
export interface Shipment {
  id: number;
  status: string;
  courier: string | null;
  trackingNumber: string | null;
  trackingUrl: string | null;
  shippedAt: string | null;
  deliveredAt: string | null;
}
export interface OrderHistory {
  previousStatus: string | null;
  newStatus: string;
  reason: string | null;
  createdAt: string;
}
export interface Order {
  id: number;
  orderNumber: string;
  status: string;
  paymentStatus: string;
  fulfilmentStatus: string;
  currency: string;
  total: number;
  subtotal: number;
  discountTotal: number;
  taxTotal: number;
  paymentFee: number;
  shippingTotal: number;
  couponCode: string | null;
  userName: string;
  userEmail: string;
  orderDate: string;
  pricesIncludeTax: boolean;
  pickupLocation: string | null;
  createdAt: string;
  shippingMethodName: string | null;
  shippingRecipientName: string | null;
  shippingPhone: string | null;
  shippingAddress: string | null;
  shippingCity: string | null;
  shippingRegion: string | null;
  shippingPostalCode: string | null;
  shippingCountry: string | null;
  notes: string | null;
  isPickup: boolean;
  orderItems: OrderItem[];
  shipments: Shipment[];
  payments: Array<{
    id: number;
    status: string;
    amount: number;
    currency: string;
    provider: string;
  }>;
  history: OrderHistory[];
  availableTransitions: string[];
}
