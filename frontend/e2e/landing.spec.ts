import { expect, test } from '@playwright/test';
import * as path from 'node:path';

// The landing page is public; every test here runs without a session.
test.use({ storageState: { cookies: [], origins: [] } });

const SHOTS = path.join(__dirname, '.artifacts');

test('landing renders for anonymous visitors', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(
    'Mochi counts visits, not people.',
  );
  await expect(page.getByRole('heading', { name: 'Everything you need' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'How visitors stay anonymous' })).toBeVisible();
  await expect(page.getByText('docker compose up -d')).toBeVisible();
});

test('header CTA navigates to login', async ({ page }) => {
  await page.goto('/');

  await page.locator('header').getByRole('link', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
});

test('github links point at the repository', async ({ page }) => {
  await page.goto('/');

  const repo = 'https://github.com/hazeliscoding/mochi';
  await expect(page.locator('header').getByRole('link', { name: 'GitHub' })).toHaveAttribute(
    'href',
    repo,
  );
  await expect(page.getByRole('link', { name: 'Self-host it' })).toHaveAttribute('href', repo);
  await expect(page.getByRole('link', { name: 'Mochi on GitHub' })).toHaveAttribute('href', repo);
});

test('renders in both themes', async ({ page }) => {
  await page.goto('/');

  // Dark is the default theme.
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await expect(page.locator('.hero__shot img')).toHaveAttribute('src', '/landing/overview.png');
  await page.screenshot({ path: path.join(SHOTS, 'landing-dark.png'), fullPage: true });

  await page.getByRole('button', { name: 'Switch to light theme' }).click();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
  await expect(page.locator('.hero__shot img')).toHaveAttribute(
    'src',
    '/landing/overview-light.png',
  );
  await page.screenshot({ path: path.join(SHOTS, 'landing-light.png'), fullPage: true });
});
