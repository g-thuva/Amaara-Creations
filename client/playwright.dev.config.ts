import { defineConfig } from '@playwright/test';
import base from './playwright.admin-ui.config';

const shop = 'http://127.0.0.1:5188/Amaara-Creations/';
const staff = 'http://127.0.0.1:5189/Amaara-Creations/';
process.env.AMAARA_E2E_STOREFRONT_URL = shop;
process.env.AMAARA_E2E_MANAGEMENT_URL = staff;

// Exercise the normal paired startup on isolated ports with real test-only MFA.
export default defineConfig({
  ...base,
  testIgnore: [],
  projects: [{ name: 'paired-development', testMatch: '**/account/staff-workspace-availability.spec.ts', use: { baseURL: shop } }],
  webServer: [
    {
      ...base.webServer[0],
      env: { AMAARA_E2E_STOREFRONT_ORIGIN: new URL(shop).origin, AMAARA_E2E_MANAGEMENT_ORIGIN: new URL(staff).origin },
    },
    {
      command: 'node scripts/dev-all.mjs --host 127.0.0.1 --port 5188 --admin-port 5189',
      url: shop, reuseExistingServer: false,
      env: { VITE_API_BASE_URL: 'http://127.0.0.1:5198/api/v1', VITE_MEDIA_BASE_URL: 'http://127.0.0.1:5198', VITE_MANAGEMENT_LOGIN_URL: staff + 'login', VITE_PUBLIC_SITE_URL: shop.replace(/\/$/, '') },
    },
  ],
});
