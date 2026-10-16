export interface CheckoutRequest {
  addressId: number;
  shippingMethodId: number | null;
  couponCode: string | null;
}
export interface CheckoutSessionRequest extends CheckoutRequest {
  notes: string;
  quoteVersion: string;
  idempotencyKey: string;
}
export interface CheckoutTotals {
  subtotal: number;
  discount: number;
  shipping: number;
  tax: number;
  paymentFee: number;
  total: number;
  currency: string;
  pricesIncludeTax: boolean;
}
export interface ShippingOption {
  id: number;
  code: string;
  name: string;
  amount: number;
  currency: string;
  estimatedMinimumDays: number | null;
  estimatedMaximumDays: number | null;
  isPickup: boolean;
  pickupLocation: string | null;
  isDevelopmentValue: boolean;
}
export interface CheckoutQuote extends CheckoutTotals {
  items: Array<{ cartItemId: number; itemType: string; name: string; quantity: number; unitPrice: number; subtotal: number; configurationSnapshotJson: string | null }>;
  availableShippingMethods: ShippingOption[];
  couponResult: { code: string; applied: boolean; errorCode: string | null; message: string | null; discount: number } | null;
  quoteVersion: string;
  expiresAt: string;
  paymentAvailable: boolean;
  canPay: boolean;
  paymentProvider: string | null;
  paymentMode: string | null;
}
