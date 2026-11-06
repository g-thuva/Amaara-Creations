import { STORE_CURRENCY } from '@/shared/config/currency.js';
export const newZone = () => ({
  name: "",
  country: "",
  region: "",
  postalCodePrefix: "",
  isActive: true,
  requiresPhone: true,
  requiresRegion: false,
  requiresPostalCode: false,
});
export const newMethod = () => ({
  name: "",
  code: "",
  shippingZoneId: "",
  currency: STORE_CURRENCY,
  basePrice: "",
  freeShippingThreshold: null,
  minimumOrderValue: null,
  maximumOrderValue: null,
  estimatedMinimumDays: null,
  estimatedMaximumDays: null,
  displayOrder: 0,
  isActive: true,
  isPickup: false,
  pickupLocation: "",
  isDevelopmentValue: false,
});
export const numeric = [
  "basePrice",
  "freeShippingThreshold",
  "minimumOrderValue",
  "maximumOrderValue",
  "estimatedMinimumDays",
  "estimatedMaximumDays",
  "displayOrder",
];

export function validateMethod(value) {
  if (!value.shippingZoneId) return 'Choose a delivery zone.';
  if (value.basePrice === '' || value.basePrice == null || !Number.isFinite(Number(value.basePrice)) || Number(value.basePrice) < 0) return 'Enter a business-approved base price of zero or more.';
  if (value.minimumOrderValue != null && value.maximumOrderValue != null && Number(value.minimumOrderValue) > Number(value.maximumOrderValue)) return 'Minimum order value cannot exceed maximum order value.';
  if (value.estimatedMinimumDays != null && value.estimatedMaximumDays != null && Number(value.estimatedMinimumDays) > Number(value.estimatedMaximumDays)) return 'Minimum delivery days cannot exceed maximum delivery days.';
  if (value.isPickup && !value.pickupLocation?.trim()) return 'Enter the customer pickup location.';
  return null;
}
