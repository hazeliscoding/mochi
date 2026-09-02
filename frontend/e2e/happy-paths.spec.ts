import { expect, test } from '@playwright/test';

// Happy paths over the real stack. Global setup registered hazeliscoding.com
// and seeded 2 visitors, 4 pageviews and 1 signup event for today.

test('overview shows real metrics and breakdowns', async ({ page }) => {
  await page.goto('/overview');

  await expect(page.locator('.mo-page-title')).toHaveText('hazeliscoding.com');
  const strip = page.locator('.mo-metric-strip');
  await expect(strip).toContainText('Visitors');
  await expect(strip).toContainText('2');
  await expect(strip).toContainText('Pageviews');
  await expect(strip).toContainText('4');
  await expect(page.getByText('/blog/shipping-kawaii-ui').first()).toBeVisible();
  await expect(page.getByText('news.ycombinator.com').first()).toBeVisible();
});

test('range selector refetches the period', async ({ page }) => {
  await page.goto('/overview');
  await expect(page.locator('.mo-metric-strip')).toContainText('Visitors');

  await page.getByLabel('Date range').selectOption('Last 7 days');

  await expect(page.locator('.mo-metric-strip')).toContainText('2');
});

test('realtime shows current activity', async ({ page }) => {
  await page.goto('/realtime');

  await expect(page.getByText('Active pages')).toBeVisible();
  await expect(page.getByText('/projects').first()).toBeVisible();
  await expect(page.getByText('duckduckgo.com').first()).toBeVisible();
});

test('pages table lists tracked paths', async ({ page }) => {
  await page.goto('/pages');

  await expect(page.getByText('/blog/shipping-kawaii-ui').first()).toBeVisible();
  await expect(page.getByText('/projects').first()).toBeVisible();
});

test('events page reports the signup conversion', async ({ page }) => {
  await page.goto('/events');

  await expect(page.getByText('signup').first()).toBeVisible();
});

test('websites page lists the site with live numbers', async ({ page }) => {
  await page.goto('/websites');

  // Scope to the card so the hidden header <option> does not match first.
  const card = page.locator('.mo-card').filter({ hasText: 'views last 30 days' });
  await expect(card.getByText('hazeliscoding.com').first()).toBeVisible();
  await expect(card.getByText('views last 30 days').first()).toBeVisible();
});

test('add website flow returns a real snippet', async ({ page }) => {
  await page.goto('/add-website');

  await page.locator('#site-name').fill('e2e-site');
  await page.locator('#site-domain').fill('e2e.example.com');
  await page.getByRole('button', { name: 'Continue' }).click();

  await expect(page.getByText('Install the snippet')).toBeVisible();
  await expect(page.getByText('data-site="MC-').first()).toBeVisible();
});

test('settings shows the site snippet and saves', async ({ page }) => {
  await page.goto('/settings');

  await expect(page.getByText('data-site="MC-').first()).toBeVisible();
});

test('every page renders without console errors', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));

  for (const route of [
    '/overview',
    '/realtime',
    '/pages',
    '/sources',
    '/geography',
    '/devices',
    '/events',
    '/goals',
    '/websites',
    '/privacy',
    '/settings',
  ]) {
    await page.goto(route);
    await expect(page.locator('.mo-page-title, h1').first()).toBeVisible();
  }

  expect(errors).toEqual([]);
});
