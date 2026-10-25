export function paymentBlocker({ quote, shippingMethodId, pending }) {
  if (pending) return null;
  if (quote.loading) return 'Updating your checkout total. Please wait.';
  if (quote.error) return 'Refresh your checkout quote before continuing to payment.';
  if (!quote.data) return 'Choose a saved delivery address to continue.';
  if (!quote.data.availableShippingMethods.length) return 'Choose an address with an available delivery option before continuing to payment.';
  if (!shippingMethodId) return 'Choose a delivery option to continue to payment.';
  if (quote.data.couponResult?.applied === false) return 'Remove the optional coupon or apply a valid code to continue.';
  if (!quote.data.paymentAvailable) return 'Online payment is not enabled for this checkout. Please contact Amaara to complete your order. Your cart has not been charged.';
  if (!quote.data.canPay) return 'Refresh your checkout quote to confirm payment availability.';
  return null;
}
