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
  basePrice: 0,
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
