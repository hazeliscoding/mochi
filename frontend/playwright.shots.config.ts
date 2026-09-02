import { defineConfig } from '@playwright/test';

// Screenshot capture for docs/screenshots. Run on demand with
// `npm run shots`; not part of the e2e suite or CI.
export default defineConfig({
  testDir: './shots',
  timeout: 120_000,
  retries: 0,
  workers: 1,
  globalSetup: './e2e/global-setup',
  use: {
    baseURL: 'http://localhost:4400',
    storageState: 'e2e/.auth/state.json',
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 2,
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
