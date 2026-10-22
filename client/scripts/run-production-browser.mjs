import { spawnSync } from 'node:child_process';
const result = spawnSync(process.execPath, ['node_modules/@playwright/test/cli.js', 'test',
  'e2e/production/release-production.spec.ts', 'e2e/admin/authentication.spec.ts', 'e2e/admin/enrollment.spec.ts', 'e2e/admin/origin-boundary.spec.ts', 'e2e/account/authentication-races.spec.ts', 'e2e/storefront/redesign.spec.ts', 'e2e/storefront/accessibility.spec.ts',
  'e2e/storefront/editor-layout.spec.ts', 'e2e/storefront/variants.spec.ts', 'e2e/account/security-settings.spec.ts',
  'e2e/account/privacy-requests.spec.ts', 'e2e/account/avatar-crop.spec.ts', 'e2e/account/responsive.spec.ts', 'e2e/checkout/cart-drawer.spec.ts', 'e2e/storefront/upload-transfer.spec.ts'], {
  env: { ...process.env, AMAARA_E2E_PRODUCTION: '1' }, stdio: 'inherit',
});
process.exitCode = result.status ?? 1;
