export { paymentPresentation } from '../src/features/payments/paymentNavigation.js';

// Served only by Vite's explicit local simulator mode; never imported by a build.
export function redirectToPayment(url, navigate = target => window.location.assign(target)) {
  const target = new URL(url);
  if (target.origin !== 'https://payments.simulator.invalid' || !/^\/pay\/[0-9]{10,30}$/.test(target.pathname) || target.search || target.hash || target.username || target.password) {
    throw new Error('Only this local simulator payment address is allowed.');
  }
  navigate('http://127.0.0.1:5208/simulator' + target.pathname);
}
