import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './e2e', workers: 1, fullyParallel: false, timeout: 60000,
  use: { baseURL: 'http://localhost:5173/Amaara-Creations/', channel: 'chrome', trace: 'off', screenshot: 'off', video: 'off' },
  reporter: 'list',
});
