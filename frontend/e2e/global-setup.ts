import { request, type APIRequestContext } from '@playwright/test';
import * as fs from 'node:fs';
import * as path from 'node:path';

const API = 'http://localhost:5000';
const STATE_PATH = path.join(__dirname, '.auth', 'state.json');

export const E2E_EMAIL = 'e2e@mochi.test';
export const E2E_PASSWORD = 'e2e-password-1';
const SETUP_CODE = 'e2e-setup-code';

const FIREFOX = 'Mozilla/5.0 (Windows NT 10.0; rv:130.0) Gecko/20100101 Firefox/130.0';
const IPHONE =
  'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1';

async function collect(userAgent: string, payload: object): Promise<void> {
  const resp = await fetch(`${API}/api/collect`, {
    method: 'POST',
    headers: { 'Content-Type': 'text/plain', 'User-Agent': userAgent },
    body: JSON.stringify(payload),
  });
  if (resp.status !== 202) throw new Error(`collect returned ${resp.status}`);
}

/** GET /api/auth/status plants the XSRF cookie; non-GET calls must echo it as a header. */
async function xsrfToken(ctx: APIRequestContext): Promise<string> {
  const status = await ctx.get(`${API}/api/auth/status`);
  if (!status.ok()) throw new Error(`auth status returned ${status.status()}`);
  const cookie = (await ctx.storageState()).cookies.find(c => c.name === 'XSRF-TOKEN');
  if (!cookie) throw new Error('no XSRF-TOKEN cookie after /api/auth/status');
  return cookie.value;
}

// Creates the admin account, registers the demo site and seeds traffic so
// every page has data. The API runs in-memory, so a fresh server starts from
// a clean slate; a reused server is already set up and gets a login instead.
export default async function globalSetup(): Promise<void> {
  const ctx = await request.newContext();
  const headers = { 'X-XSRF-TOKEN': await xsrfToken(ctx) };

  const setup = await ctx.post(`${API}/api/auth/setup`, {
    headers,
    data: { code: SETUP_CODE, email: E2E_EMAIL, password: E2E_PASSWORD },
  });
  if (!setup.ok()) {
    const login = await ctx.post(`${API}/api/auth/login`, {
      headers,
      data: { email: E2E_EMAIL, password: E2E_PASSWORD },
    });
    if (!login.ok()) throw new Error(`setup returned ${setup.status()}, login returned ${login.status()}`);
  }

  const resp = await ctx.post(`${API}/api/sites`, {
    headers,
    data: { name: 'hazeliscoding', domain: 'hazeliscoding.com', timezone: 'Europe/Berlin' },
  });
  if (resp.status() !== 201) throw new Error(`site registration returned ${resp.status()}`);
  const site = (await resp.json()) as { id: string };

  await collect(FIREFOX, { site: site.id, type: 'pageview', path: '/', referrer: 'https://news.ycombinator.com/item?id=1' });
  await collect(FIREFOX, { site: site.id, type: 'pageview', path: '/blog/shipping-kawaii-ui' });
  await collect(FIREFOX, { site: site.id, type: 'event', name: 'signup', path: '/blog/shipping-kawaii-ui' });
  await collect(IPHONE, { site: site.id, type: 'pageview', path: '/', referrer: 'https://duckduckgo.com/' });
  await collect(IPHONE, { site: site.id, type: 'pageview', path: '/projects' });

  process.env['MOCHI_E2E_SITE_ID'] = site.id;

  fs.mkdirSync(path.dirname(STATE_PATH), { recursive: true });
  await ctx.storageState({ path: STATE_PATH });
  await ctx.dispose();
}
