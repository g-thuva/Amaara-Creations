import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { checkoutApi, checkoutMoney, paymentPresentation, redirectToPayment } from './checkoutApi';
import CheckoutTotals from '../components/storefront/CheckoutTotals';
import api from './api';
vi.mock('./api', () => ({ default: { post: vi.fn(), get: vi.fn() } }));

describe('secure checkout client boundary', () => {
  it('passes request cancellation through quote and status reads', async () => {
    const signal = new AbortController().signal;
    api.post.mockResolvedValue({ data: { total: 225 } }); api.get.mockResolvedValue({ data: { status: 'Pending' } });
    expect(await checkoutApi.quote({ addressId: 1 }, signal)).toEqual({ total: 225 });
    await checkoutApi.payment(3, signal);
    expect(api.post).toHaveBeenCalledWith('/checkout/quote', { addressId: 1 }, { signal });
    expect(api.get).toHaveBeenCalledWith('/payments/3', { signal });
  });
  it('navigates only to a provider-controlled HTTPS hosted payment URL', () => {
    const navigate = vi.fn(); const url = 'https://pay.zahls.ch/TEST-ONLY-1';
    redirectToPayment(url, navigate); expect(navigate).toHaveBeenCalledWith(url);
    const instanceUrl = 'https://merchant.zahls.ch/pay?tid=TEST-ONLY-1';
    redirectToPayment(instanceUrl, navigate); expect(navigate).toHaveBeenCalledWith(instanceUrl);
    for (const value of ['javascript:alert(1)', 'https://pay.zahls.ch.evil.example/pay', url + '#x', url.replace('https:', 'http:'), url.replace('https://', 'https://user:password@'), url.replace('.ch/', '.ch:8443/'), 'https://api.zahls.ch/Gateway/']) {
      expect(() => redirectToPayment(value, navigate)).toThrow();
    }
    expect(navigate).toHaveBeenCalledTimes(2);
  });
  it('shows confirmation only for server verified payment states', () => {
    expect(paymentPresentation(null).title).toBe('Verifying your payment');
    for (const status of ['Pending', 'Authorized', 'Unknown']) expect(paymentPresentation({ status }).confirmed).toBeUndefined();
    for (const status of ['Paid', 'PartiallyRefunded', 'Refunded']) expect(paymentPresentation({ status }).confirmed).toBe(true);
    expect(paymentPresentation({ status: 'Paid', orderStatus: 'PaymentReview' }).confirmed).toBeUndefined();
    expect(paymentPresentation({ status: 'Failed' }).retry).toBe(true);
    expect(paymentPresentation({ status: 'Cancelled' }).retry).toBe(true);
    expect(paymentPresentation({ status: 'Pending', unableToVerify: true }).title).toBe('Unable to verify yet');
  });
  it('renders the server total and labels inclusive tax without recomputing it', () => {
    const markup = renderToStaticMarkup(<CheckoutTotals quote={{ subtotal: 200, discount: 10, shipping: 25, tax: 15, paymentFee: 3, total: 218, currency: 'LKR', pricesIncludeTax: true }} />);
    expect(markup).toContain('218.00'); expect(markup).toContain('Tax (included)'); expect(markup).toContain('LKR'); expect(markup).toContain('-LKR');
    expect(checkoutMoney(10, 'CHF')).toContain('CHF');
  });
});
