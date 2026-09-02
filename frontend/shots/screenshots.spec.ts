import { expect, test, type Page } from '@playwright/test';
import * as fs from 'node:fs';
import * as path from 'node:path';

// Captures the README screenshots into docs/screenshots. Seeds richer traffic
// than the e2e setup so the dashboards look representative.

const OUT = path.join(__dirname, '..', '..', 'docs', 'screenshots');
const API = 'http://localhost:5000';

const UAS = [
  'Mozilla/5.0 (Windows NT 10.0; rv:130.0) Gecko/20100101 Firefox/130.0',
  'Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36',
  'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1',
  'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36 Edg/128.0',
  'Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Mobile Safari/537.36',
];

const VISITS: Array<[path: string, referrer?: string]> = [
  ['/', 'https://news.ycombinator.com/item?id=1'],
  ['/blog/shipping-kawaii-ui', 'https://news.ycombinator.com/item?id=1'],
  ['/projects'],
  ['/', 'https://duckduckgo.com/'],
  ['/pricing', 'https://www.google.com/'],
  ['/blog', 'https://reddit.com/r/webdev'],
  ['/about'],
  ['/pricing?utm_campaign=launch', 'https://github.com/hazeliscoding'],
  ['/uses'],
  ['/blog/why-i-left-big-analytics', 'https://bsky.app/'],
];

async function collect(userAgent: string, body: object): Promise<void> {
  const resp = await fetch(`${API}/api/collect`, {
    method: 'POST',
    headers: { 'Content-Type': 'text/plain', 'User-Agent': userAgent },
    body: JSON.stringify(body),
  });
  if (resp.status !== 202) throw new Error(`collect returned ${resp.status}`);
}

async function capture(
  page: Page,
  theme: 'dark' | 'light',
  route: string,
  waitFor: string,
  name: string,
): Promise<void> {
  await page.addInitScript((t) => localStorage.setItem('mochi-theme', t), theme);
  await page.goto(route);
  await expect(page.locator(waitFor).first()).toBeVisible({ timeout: 30_000 });
  await page.waitForTimeout(1200);
  await page.screenshot({ path: path.join(OUT, name), fullPage: false });
}

test('capture readme screenshots', async ({ page, context }) => {
  fs.mkdirSync(OUT, { recursive: true });

  const siteId = process.env['MOCHI_E2E_SITE_ID']!;
  for (const ua of UAS) {
    for (const [p, referrer] of VISITS) {
      await collect(ua, { site: siteId, type: 'pageview', path: p, referrer });
    }
  }
  for (const ua of UAS.slice(0, 3)) {
    await collect(ua, { site: siteId, type: 'event', name: 'signup', path: '/pricing' });
    await collect(ua, {
      site: siteId,
      type: 'event',
      name: 'github_link_clicked',
      path: '/projects',
    });
  }

  const xsrf = (await context.cookies()).find((c) => c.name === 'XSRF-TOKEN')!.value;
  for (const goal of [
    { name: 'Signed up', type: 'event', target: 'signup' },
    { name: 'Saw pricing', type: 'page', target: '/pricing' },
  ]) {
    await page.request.post(`${API}/api/sites/${siteId}/goals`, {
      headers: { 'X-XSRF-TOKEN': xsrf },
      data: goal,
    });
  }

  const pages: Array<[route: string, waitFor: string, base: string]> = [
    ['/overview', '.mo-metric-strip', 'overview'],
    ['/realtime', 'text=Active pages', 'realtime'],
    ['/goals', 'text=Signed up', 'goals'],
    ['/privacy', 'text=WHAT MOCHI HOLDS RIGHT NOW', 'privacy'],
  ];

  for (const [route, waitFor, base] of pages) {
    await capture(page, 'dark', route, waitFor, `${base}.png`);
  }
  const light = await context.newPage();
  for (const [route, waitFor, base] of pages) {
    await capture(light, 'light', route, waitFor, `${base}-light.png`);
  }
});
