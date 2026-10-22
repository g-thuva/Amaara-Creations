import { defineConfig } from '@playwright/test';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import base from './playwright.config';

// Keep test-host build outputs separate from a running local business API.
const root = fileURLToPath(new URL('../', import.meta.url));
const artifacts = path.join(root, '.runtime', 'admin-ui-acceptance-build');
const project = path.join(root, 'tests', 'be.AcceptanceHost', 'be.AcceptanceHost.csproj');
const assembly = path.join(artifacts, 'bin', 'be.AcceptanceHost', 'debug', 'be.AcceptanceHost.dll');
export default defineConfig({
  ...base,
  webServer: base.webServer.map((server, index) => index === 0 ? {
    ...server,
    command: `dotnet build "${project}" --configuration Debug --artifacts-path "${artifacts}" --verbosity quiet && dotnet "${assembly}"`,
  } : server),
  // These four harnesses intentionally import /src modules and run in dev mode.
  grepInvert: process.env.AMAARA_E2E_PRODUCTION === '1'
    ? /product hook rejects older filter results|admin mutation hook locks synchronous|late pre-refund response|resource refresh error retains the last server snapshot/
    : undefined,
});
