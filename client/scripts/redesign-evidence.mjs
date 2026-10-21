import { chromium } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import process from 'node:process';

const stage = process.argv[2] || 'before';
const directory = `../docs/redesign/evidence/${stage}`;
await mkdir(directory, { recursive: true });
const browser = await chromium.launch({ channel: process.platform === 'win32' ? 'chrome' : undefined });
const page = await browser.newPage();
const notes = [];
try {
  for (const width of [390, 768, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    for (const route of ['', 'products', 'custom', 'login']) {
      const response = await page.goto(`http://127.0.0.1:5178/Amaara-Creations/${route}`);
      await page.locator('h1').first().waitFor();
      await page.screenshot({ path: `${directory}/${route || 'home'}-${width}.png`, fullPage: true });
      notes.push({ route: route || 'home', width, status: response.status(), overflow: await page.evaluate(() => document.documentElement.scrollWidth > innerWidth) });
    }
  }
  if (stage === 'before') {
    const urls = ['https://www.zazzle.com/c/wedding+signs', 'https://www.trulyengaging.com/shop/wedding-signs', 'https://www.etsy.com/', 'https://www.etsy.com/market/acrylic_wedding_sign', 'https://icustomlabel.com/collections/wedding-signs', 'https://lunavows.com/collections/signs-decor', 'https://www.anim8.lk/', 'https://madeandtrue.com/', 'https://g-thuva.github.io/Amaara-Creations/#/'];
    for (const url of urls) {
      for (const width of [1440, 768, 390]) {
        await page.setViewportSize({ width, height: 1000 });
        try {
          const response = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 12000 });
          notes.push({ url, width, status: response?.status(), title: await page.title(), headings: await page.locator('h1,h2').allTextContents() });
        } catch (error) { notes.push({ url, width, blocked: error.message.split('\n')[0] }); }
      }
    }
  }
} finally {
  await writeFile(`${directory}/observations.json`, JSON.stringify(notes, null, 2));
  await browser.close();
}
console.log(`Evidence written to ${directory}`);
