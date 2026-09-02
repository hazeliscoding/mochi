import { expect, test } from '@playwright/test';

// Goals happy path against the real API. Global setup seeded one signup event
// from one of two visitors, so an event goal on "signup" converts at 50%.

test('create a goal, see conversions, delete it', async ({ page }) => {
  await page.goto('/goals');
  await expect(page.getByText('No goals yet')).toBeVisible();

  await page.getByRole('button', { name: 'Create goal' }).click();
  const dialog = page.locator('.tr-dialog');
  await dialog.getByText('Custom event').click();
  await page.locator('#goal-name').fill('Signed up');
  await page.locator('#goal-target').fill('signup');
  await dialog.getByRole('button', { name: 'Create goal' }).click();

  const row = page.locator('.mo-grid-table__row', { hasText: 'Signed up' });
  await expect(row).toBeVisible();
  await expect(row).toContainText('Custom event');
  await expect(row).toContainText('1');
  await expect(row).toContainText('50.0%');

  await row.getByRole('button', { name: 'Delete Signed up' }).click();
  await page.getByRole('button', { name: 'Delete goal' }).click();
  await expect(page.getByText('No goals yet')).toBeVisible();
});
