import AxeBuilder from '@axe-core/playwright';
import { expect } from '@playwright/test';
const tags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
export async function checkAccessibility(page: import('@playwright/test').Page, route: string) {
  await page.goto('./' + route); await expect(page.locator('main h1')).toBeVisible();
  await expect(page.locator('[aria-label="Loading content"]')).toHaveCount(0);
  await page.evaluate(() => document.fonts.ready);
  const result = await new AxeBuilder({ page }).withTags(tags).analyze();
  expect(result.violations.map(v => ({ id: v.id, impact: v.impact, nodes: v.nodes.map(n => ({ target: n.target, summary: n.failureSummary })) })), route).toEqual([]);
}
