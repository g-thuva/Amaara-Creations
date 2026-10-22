import { defineConfig } from '@playwright/test';
export default defineConfig({
  testIgnore: process.env.AMAARA_E2E_PRODUCTION === '1' ? undefined : '**/release-production.spec.ts',
  testDir: './e2e', workers: 1, fullyParallel: false, timeout: 60000,
  projects: [
    { name: 'storefront', testIgnore: ['**/admin/**', ...(process.env.AMAARA_E2E_PRODUCTION === '1' ? [] : ['**/release-production.spec.ts'])], use: { baseURL: 'http://127.0.0.1:5178/Amaara-Creations/' } },
    { name: 'staff', testMatch: '**/admin/**/*.spec.ts', use: { baseURL: 'http://127.0.0.1:5179/Amaara-Creations/' } },
  ],
  use: { baseURL: 'http://127.0.0.1:5178/Amaara-Creations/', channel: process.platform === 'win32' ? 'chrome' : undefined, actionTimeout: 15000, navigationTimeout: 30000, trace: 'off', screenshot: 'off', video: 'off' },
  webServer: [
    {
      command: 'dotnet run --project ../tests/be.AcceptanceHost/be.AcceptanceHost.csproj --configuration Release --no-launch-profile',
      url: 'http://127.0.0.1:5198/__fixture/health',
      reuseExistingServer: false, timeout: 120000,
    },
    {
      command: process.env.AMAARA_E2E_PRODUCTION === '1'
        ? 'node node_modules/vite/bin/vite.js build --mode admin && node node_modules/vite/bin/vite.js preview --mode admin --host 127.0.0.1 --port 5179 --strictPort'
        : 'node node_modules/vite/bin/vite.js --mode admin --host 127.0.0.1 --port 5179 --strictPort',
      url: 'http://127.0.0.1:5179/Amaara-Creations/',
      env: { VITE_API_BASE_URL: 'http://127.0.0.1:5198/api/v1', VITE_MEDIA_BASE_URL: 'http://127.0.0.1:5198' },
      reuseExistingServer: false,
    },
    {
      command: process.env.AMAARA_E2E_PRODUCTION === '1'
        ? 'node node_modules/vite/bin/vite.js build && node node_modules/vite/bin/vite.js preview --host 127.0.0.1 --port 5178 --strictPort'
        : 'node node_modules/vite/bin/vite.js --host 127.0.0.1 --port 5178 --strictPort',
      url: 'http://127.0.0.1:5178/Amaara-Creations/',
      env: { VITE_API_BASE_URL: 'http://127.0.0.1:5198/api/v1', VITE_MEDIA_BASE_URL: 'http://127.0.0.1:5198', VITE_PUBLIC_SITE_URL: 'https://store.example.test/Amaara-Creations' },
      reuseExistingServer: false,
    },
  ],
  reporter: 'list',
});
