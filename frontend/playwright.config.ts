import { defineConfig } from '@playwright/test';

// E2e happy paths against the real stack: the .NET API in its in-memory dev
// mode plus ng serve with the /api proxy. Global setup creates the admin
// account (pinned setup code), seeds data and saves the session storage state.
export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  retries: 0,
  workers: 1,
  globalSetup: './e2e/global-setup',
  use: {
    baseURL: 'http://localhost:4400',
    trace: 'retain-on-failure',
    storageState: 'e2e/.auth/state.json',
  },
  webServer: [
    {
      command: 'dotnet run --no-launch-profile --urls http://localhost:5000',
      cwd: '../backend/src/Mochi.Api',
      url: 'http://localhost:5000/api/auth/status',
      env: { MOCHI_SETUP_CODE: 'e2e-setup-code' },
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command: 'npx ng serve --port 4400 --proxy-config proxy.conf.json',
      url: 'http://localhost:4400',
      reuseExistingServer: true,
      timeout: 180_000,
    },
  ],
});
