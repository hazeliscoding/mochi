import { expect, test, type Page } from '@playwright/test';

// Privacy center against the real API. Global setup seeded 5 beacons
// (4 pageviews + 1 custom event) for the site in MOCHI_E2E_SITE_ID.
// The header defaults to the newest site, so each test pins the seeded one.

const seededSiteId = (): string => process.env['MOCHI_E2E_SITE_ID']!;

async function pickSeededSite(page: Page): Promise<void> {
  await page.getByLabel('Current website').selectOption(seededSiteId());
}

test('shows the live raw-event count from seeded beacons', async ({ page }) => {
  const resp = await page.request.get(`/api/sites/${seededSiteId()}/privacy`);
  expect(resp.ok()).toBeTruthy();
  const summary = (await resp.json()) as { rawEventsHeld: number; rawEventLifetimeDays: number };
  expect(summary.rawEventsHeld).toBeGreaterThanOrEqual(5);

  await page.goto('/privacy');
  await pickSeededSite(page);
  const holds = page.locator('.mo-card', { hasText: 'What Mochi holds right now' });
  await expect(holds.getByText(summary.rawEventsHeld.toLocaleString('en-US'), { exact: true })).toBeVisible();
  await expect(holds).toContainText(`${summary.rawEventLifetimeDays} days`);
});

test('changing retention persists and Settings shows the same value', async ({ page }) => {
  await page.goto('/privacy');
  await pickSeededSite(page);
  await page.getByRole('radio', { name: /^90 days/ }).check();
  await expect(page.getByText('Saved.')).toBeVisible();

  await page.reload();
  await pickSeededSite(page);
  await expect(page.getByRole('radio', { name: /^90 days/ })).toBeChecked();
  const holds = page.locator('.mo-card', { hasText: 'What Mochi holds right now' });
  await expect(holds).toContainText('90 days');

  await page.goto('/settings');
  await pickSeededSite(page);
  await expect(page.locator('#set-retention')).toHaveValue('90d');
});

test('export downloads a zip named after the site', async ({ page }) => {
  const resp = await page.request.get(`/api/sites/${seededSiteId()}/export`);
  expect(resp.status()).toBe(200);
  expect(resp.headers()['content-type']).toBe('application/zip');
  expect(resp.headers()['content-disposition']).toContain(`mochi-export-${seededSiteId()}.zip`);

  await page.goto('/privacy');
  await pickSeededSite(page);
  const download = page.waitForEvent('download');
  await page.getByRole('link', { name: 'Export all data' }).click();
  expect((await download).suggestedFilename()).toBe(`mochi-export-${seededSiteId()}.zip`);
});
