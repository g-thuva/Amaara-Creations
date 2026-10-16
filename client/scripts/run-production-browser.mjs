import { spawnSync } from 'node:child_process';
const result = spawnSync(process.execPath, ['node_modules/@playwright/test/cli.js', 'test', 'e2e/production/release-production.spec.ts'], {
  env: { ...process.env, AMAARA_E2E_PRODUCTION: '1' }, stdio: 'inherit',
});
process.exitCode = result.status ?? 1;
