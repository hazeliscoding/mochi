const API = 'http://localhost:5000';

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

// Registers the demo site and seeds traffic so every page has data. The API
// runs in-memory, so this starts from a clean slate on every run.
export default async function globalSetup(): Promise<void> {
  const resp = await fetch(`${API}/api/sites`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'hazeliscoding', domain: 'hazeliscoding.com', timezone: 'Europe/Berlin' }),
  });
  if (resp.status !== 201) throw new Error(`site registration returned ${resp.status}`);
  const site = (await resp.json()) as { id: string };

  await collect(FIREFOX, { site: site.id, type: 'pageview', path: '/', referrer: 'https://news.ycombinator.com/item?id=1' });
  await collect(FIREFOX, { site: site.id, type: 'pageview', path: '/blog/shipping-kawaii-ui' });
  await collect(FIREFOX, { site: site.id, type: 'event', name: 'signup', path: '/blog/shipping-kawaii-ui' });
  await collect(IPHONE, { site: site.id, type: 'pageview', path: '/', referrer: 'https://duckduckgo.com/' });
  await collect(IPHONE, { site: site.id, type: 'pageview', path: '/projects' });

  process.env['MOCHI_E2E_SITE_ID'] = site.id;
}
