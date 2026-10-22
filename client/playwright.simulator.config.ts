import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './e2e-simulator', workers: 1, fullyParallel: false, timeout: 60000,
  use: { baseURL: 'http://127.0.0.1:5188/Amaara-Creations/', channel: process.platform === 'win32' ? 'chrome' : undefined, screenshot: 'off', video: 'off', trace: 'off', actionTimeout: 15000 },
  reporter: 'list',
});
