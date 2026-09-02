import { expect, test } from '@playwright/test';
import { E2E_EMAIL, E2E_PASSWORD } from './global-setup';

// Login and logout through the real UI. This file runs anonymously and signs
// in via the form, so logging out here never kills the shared seeded session.
test.use({ storageState: { cookies: [], origins: [] } });

test('anonymous visitor is redirected to login', async ({ page }) => {
  await page.goto('/overview');
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
});

test('login, logout, login round trip', async ({ page }) => {
  await page.goto('/login');

  // Wrong password surfaces the server's message.
  await page.locator('#login-email').fill(E2E_EMAIL);
  await page.locator('#login-password').fill('wrong-password');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByText('invalid email or password')).toBeVisible();

  await page.locator('#login-password').fill(E2E_PASSWORD);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/overview$/);
  await expect(page.locator('.mo-page-title')).toHaveText('hazeliscoding.com');

  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/login$/);

  await page.locator('#login-email').fill(E2E_EMAIL);
  await page.locator('#login-password').fill(E2E_PASSWORD);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/overview$/);
  await expect(page.locator('.mo-metric-strip')).toContainText('Visitors');
});
