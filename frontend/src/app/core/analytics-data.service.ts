import { Injectable, signal } from '@angular/core';

/**
 * Mock analytics data, ported 1:1 from the approved design.
 * This service is the seam for the future .NET backend: replace the
 * constants and generated series with HTTP calls, keep the shapes.
 */

export interface PageStats {
  id: string;
  title: string;
  v: number;
  pv: number;
  bounce: number;
  dur: string;
  entry: number;
  exit: number;
}

export interface NameVal {
  name: string;
  val: string;
}

export interface BarRow {
  name: string;
  val: string;
  pct: number;
}

export interface EventStats {
  id: string;
  total: number;
  uniq: number;
  conv: string;
  delta: string;
  pages: [string, number][];
  sources: [string, number][];
}

export interface GoalStats {
  name: string;
  type: string;
  conv: number;
  rate: string;
}

export interface SiteInfo {
  name: string;
  domain: string;
  views: string;
  active: string;
  tone: 'success' | 'warning';
  status: string;
  f: number;
}

export interface TableColumn {
  key: string;
  label: string;
  numeric?: boolean;
}

function seeded(seed: number): () => number {
  let s = seed;
  return () => ((s = (s * 16807) % 2147483647) / 2147483647);
}

export function fmt(n: number): string {
  return n.toLocaleString('en-US');
}

@Injectable({ providedIn: 'root' })
export class AnalyticsDataService {
  // ── Global header state ────────────────────────────────────────────
  readonly site = signal('hazeliscoding.com');
  readonly range = signal('Last 30 days');
  readonly compare = signal('vs previous period');

  readonly rangeOptions = ['Last 7 days', 'Last 30 days', 'Last 90 days', 'Year to date'];
  readonly compareOptions = ['vs previous period', 'vs same period last year', 'No comparison'];

  // ── Daily series (30 days, seeded — matches the design exactly) ────
  readonly visitors: number[] = [];
  readonly pageviews: number[] = [];
  readonly sessions: number[] = [];
  readonly prev: number[] = [];

  constructor() {
    const r1 = seeded(42);
    const r2 = seeded(1337);
    for (let i = 0; i < 30; i++) {
      const spike = i === 18 ? 430 : i === 19 ? 180 : 0;
      const v = Math.round(170 + i * 2.6 + r1() * 75 + spike);
      this.visitors.push(v);
      this.pageviews.push(Math.round(v * (1.75 + r1() * 0.35)));
      this.sessions.push(Math.round(v * (1.05 + r1() * 0.12)));
      this.prev.push(Math.round(160 + i * 2.2 + r2() * 70));
    }
  }

  // ── Overview ───────────────────────────────────────────────────────
  readonly metrics = [
    { label: 'Visitors', value: '8,214', delta: '+12.4%', dir: 'up', tip: 'Unique visits in the period. A visit is a browsing session, not a persistent person.' },
    { label: 'Pageviews', value: '15,630', delta: '+8.1%', dir: 'up', tip: 'Total pages loaded.' },
    { label: 'Views per visitor', value: '1.9', delta: '+0.2', dir: 'up', tip: 'Pageviews divided by visitors.' },
    { label: 'Bounce rate', value: '42%', delta: '−2.1 pt', dir: 'down', tip: 'Visits that left after a single page. Lower is usually better.' },
    { label: 'Avg visit duration', value: '1m 46s', delta: '+6s', dir: 'up', tip: 'Median time between first and last pageview of a visit.' },
  ] as const;

  readonly channelRows: BarRow[] = [
    { name: 'Direct', pct: 38, val: '3,121' },
    { name: 'Search', pct: 29, val: '2,383' },
    { name: 'Referral', pct: 24, val: '1,972' },
    { name: 'Social', pct: 9, val: '738' },
  ];

  readonly topSourceRows: NameVal[] = [
    { name: 'Google', val: '2,210' },
    { name: 'GitHub', val: '842' },
    { name: 'Reddit', val: '512' },
    { name: 'Hacker News', val: '460' },
    { name: 'Bing', val: '173' },
  ];

  readonly browserTop = [
    { name: 'Chrome', pct: 46 },
    { name: 'Firefox', pct: 21 },
    { name: 'Safari', pct: 19 },
  ];

  readonly osTop = [
    { name: 'macOS', pct: 34 },
    { name: 'Windows', pct: 31 },
    { name: 'iOS', pct: 16 },
  ];

  // ── Pages ──────────────────────────────────────────────────────────
  readonly pages: PageStats[] = [
    { id: '/', title: 'Home', v: 3120, pv: 4890, bounce: 38, dur: '1m 12s', entry: 2980, exit: 1450 },
    { id: '/projects', title: 'Projects', v: 2140, pv: 3620, bounce: 31, dur: '2m 04s', entry: 640, exit: 780 },
    { id: '/blog', title: 'Blog', v: 1730, pv: 3480, bounce: 44, dur: '2m 31s', entry: 890, exit: 610 },
    { id: '/blog/shipping-kawaii-ui', title: 'Shipping Kawaii UI', v: 1260, pv: 1410, bounce: 52, dur: '3m 18s', entry: 1120, exit: 940 },
    { id: '/projects/kawaii-ui', title: 'Kawaii UI', v: 840, pv: 1680, bounce: 29, dur: '2m 47s', entry: 310, exit: 260 },
    { id: '/about', title: 'About', v: 960, pv: 1150, bounce: 41, dur: '1m 05s', entry: 220, exit: 480 },
    { id: '/uses', title: 'Uses', v: 410, pv: 470, bounce: 55, dur: '0m 58s', entry: 130, exit: 210 },
    { id: '/blog/why-i-left-big-analytics', title: 'Why I left big analytics', v: 380, pv: 425, bounce: 48, dur: '4m 02s', entry: 290, exit: 250 },
  ];

  pageDetailSeries(page: PageStats): number[] {
    return this.visitors.map((v, i) => Math.round(v * (page.v / 8214) * (0.8 + ((i * 7) % 5) / 10)));
  }

  readonly selPageRefs: NameVal[] = [
    { name: 'Direct', val: '38%' },
    { name: 'google.com', val: '27%' },
    { name: 'github.com', val: '19%' },
    { name: 'reddit.com', val: '9%' },
  ];
  readonly selPageDevices: NameVal[] = [
    { name: 'Desktop', val: '61%' },
    { name: 'Mobile', val: '33%' },
    { name: 'Tablet', val: '6%' },
  ];
  readonly selPageCountries: NameVal[] = [
    { name: 'United States', val: '34%' },
    { name: 'Germany', val: '15%' },
    { name: 'United Kingdom', val: '11%' },
    { name: 'Japan', val: '9%' },
  ];
  readonly selPageEvents: NameVal[] = [
    { name: 'project_link_clicked', val: '412' },
    { name: 'github_link_clicked', val: '186' },
  ];

  // ── Sources ────────────────────────────────────────────────────────
  readonly channelShares = { direct: 0.38, search: 0.29, referral: 0.24, social: 0.09 };

  readonly sourceTables: Record<string, { cols: TableColumn[]; rows: Record<string, string>[] }> = {
    referrers: {
      cols: [
        { key: 'name', label: 'Referrer' },
        { key: 'v', label: 'Visitors', numeric: true },
        { key: 'pv', label: 'Pageviews', numeric: true },
        { key: 'bounce', label: 'Bounce', numeric: true },
      ],
      rows: [
        { id: 'gh', name: 'github.com', v: '842', pv: '1,690', bounce: '28%' },
        { id: 'go', name: 'google.com', v: '2,210', pv: '3,980', bounce: '43%' },
        { id: 'rd', name: 'reddit.com', v: '512', pv: '890', bounce: '51%' },
        { id: 'hn', name: 'news.ycombinator.com', v: '460', pv: '1,120', bounce: '39%' },
        { id: 'bg', name: 'bing.com', v: '173', pv: '260', bounce: '47%' },
        { id: 'dev', name: 'dev.to', v: '128', pv: '245', bounce: '35%' },
      ],
    },
    search: {
      cols: [
        { key: 'name', label: 'Search engine' },
        { key: 'v', label: 'Visitors', numeric: true },
        { key: 'share', label: 'Share', numeric: true },
      ],
      rows: [
        { id: 's1', name: 'Google', v: '2,210', share: '89%' },
        { id: 's2', name: 'Bing', v: '173', share: '7%' },
        { id: 's3', name: 'DuckDuckGo', v: '96', share: '4%' },
      ],
    },
    social: {
      cols: [
        { key: 'name', label: 'Platform' },
        { key: 'v', label: 'Visitors', numeric: true },
        { key: 'pv', label: 'Pageviews', numeric: true },
      ],
      rows: [
        { id: 'p1', name: 'Reddit', v: '512', pv: '890' },
        { id: 'p2', name: 'Mastodon', v: '134', pv: '250' },
        { id: 'p3', name: 'Bluesky', v: '92', pv: '160' },
      ],
    },
    campaigns: {
      cols: [
        { key: 'name', label: 'Campaign' },
        { key: 'sm', label: 'Source / medium' },
        { key: 'v', label: 'Visitors', numeric: true },
        { key: 'conv', label: 'Conversions', numeric: true },
      ],
      rows: [
        { id: 'c1', name: 'newsletter-aug', sm: 'buttondown / email', v: '284', conv: '41' },
        { id: 'c2', name: 'kawaii-ui-launch', sm: 'reddit / social', v: '196', conv: '22' },
        { id: 'c3', name: 'conf-talk-qr', sm: 'qr / offline', v: '58', conv: '9' },
      ],
    },
  };

  // ── Geography ──────────────────────────────────────────────────────
  readonly geo: [string, number, number][] = [
    ['United States', 2870, 35], ['Germany', 1150, 14], ['United Kingdom', 985, 12],
    ['Japan', 740, 9], ['Canada', 660, 8], ['Netherlands', 420, 5],
    ['Australia', 390, 5], ['France', 310, 4], ['Other', 689, 8],
  ];

  readonly usRegions: NameVal[] = [
    { name: 'California', val: '412' }, { name: 'New York', val: '268' }, { name: 'Texas', val: '214' },
    { name: 'Washington', val: '176' }, { name: 'Massachusetts', val: '121' }, { name: 'Other regions', val: '1,679' },
  ];

  /** Stylized world-map dot grid: per row, [startCol, endCol] land ranges. */
  readonly mapLand: [number, number][][] = [
    [[2, 9], [16, 20], [32, 58]], [[1, 10], [16, 20], [30, 58]], [[2, 12], [17, 19], [29, 33], [34, 58]],
    [[3, 13], [28, 34], [35, 57]], [[3, 14], [28, 36], [37, 55]], [[3, 14], [28, 37], [38, 54]],
    [[3, 13], [28, 38], [39, 52], [53, 54]], [[4, 12], [27, 40], [41, 50], [52, 53]],
    [[4, 10], [26, 42], [42, 50]], [[4, 8], [26, 40], [42, 46], [47, 50]],
    [[5, 8], [25, 36], [42, 45], [46, 50]], [[6, 8], [25, 35], [43, 44], [46, 51]],
    [[7, 10], [25, 34], [47, 53]], [[8, 12], [25, 33], [48, 55]], [[8, 13], [26, 33], [49, 56]],
    [[8, 13], [26, 32]], [[8, 13], [27, 32], [51, 56]], [[8, 12], [27, 31], [50, 57]],
    [[9, 12], [28, 31], [50, 57]], [[9, 11], [28, 30], [51, 56]], [[9, 11], [28, 30]],
    [[9, 10]], [[9, 10]], [[9, 9]],
  ];

  /** Highlight regions: [rowStart, rowEnd, colStart, colEnd, intensity]. */
  readonly mapHighlights: [number, number, number, number, number][] = [
    [5, 7, 4, 12, 1], [2, 4, 4, 12, 0.5], [3, 3, 28, 29, 0.8], [4, 4, 31, 32, 0.85],
    [4, 4, 30, 30, 0.5], [5, 5, 29, 30, 0.45], [6, 7, 52, 54, 0.75], [17, 19, 51, 55, 0.5],
  ];

  // ── Devices ────────────────────────────────────────────────────────
  readonly deviceRows = [
    { name: 'Desktop', pct: 58, val: '4,764', color: 'var(--color-accent)' },
    { name: 'Mobile', pct: 36, val: '2,957', color: 'var(--teal-300)' },
    { name: 'Tablet', pct: 6, val: '493', color: 'var(--color-border)' },
  ];

  readonly browserRows: BarRow[] = [
    { name: 'Chrome', pct: 46, val: '3,778' }, { name: 'Firefox', pct: 21, val: '1,725' },
    { name: 'Safari', pct: 19, val: '1,561' }, { name: 'Edge', pct: 8, val: '657' },
    { name: 'Arc', pct: 4, val: '329' }, { name: 'Other', pct: 2, val: '164' },
  ];

  readonly osRows: BarRow[] = [
    { name: 'macOS', pct: 34, val: '2,793' }, { name: 'Windows', pct: 31, val: '2,546' },
    { name: 'iOS', pct: 16, val: '1,314' }, { name: 'Android', pct: 12, val: '986' }, { name: 'Linux', pct: 7, val: '575' },
  ];

  // ── Realtime ───────────────────────────────────────────────────────
  readonly rtVals = Array.from({ length: 30 }, (_, i) => 2 + Math.round((Math.sin(i * 1.7) + 1) * 2.4 + ((i * 13) % 3)));

  readonly rtPages: BarRow[] = [
    { name: '/', val: '6', pct: 100 }, { name: '/projects', val: '5', pct: 83 },
    { name: '/blog/shipping-kawaii-ui', val: '4', pct: 67 }, { name: '/about', val: '3', pct: 50 },
  ];
  readonly rtSources: BarRow[] = [
    { name: 'Direct', val: '8', pct: 100 }, { name: 'GitHub', val: '5', pct: 63 },
    { name: 'Google', val: '4', pct: 50 }, { name: 'Reddit', val: '1', pct: 13 },
  ];
  readonly rtCountries: NameVal[] = [
    { name: 'United States', val: '7' }, { name: 'Germany', val: '4' }, { name: 'Japan', val: '3' },
    { name: 'United Kingdom', val: '2' }, { name: 'Other', val: '2' },
  ];

  // ── Events ─────────────────────────────────────────────────────────
  readonly events: EventStats[] = [
    {
      id: 'project_link_clicked', total: 1284, uniq: 1050, conv: '12.8%', delta: '+18%',
      pages: [['/projects', 812], ['/', 296], ['/projects/kawaii-ui', 176]],
      sources: [['Direct', 490], ['GitHub', 310], ['Google', 250]],
    },
    {
      id: 'github_link_clicked', total: 918, uniq: 812, conv: '9.9%', delta: '+11%',
      pages: [['/projects', 502], ['/about', 214], ['/', 202]],
      sources: [['Direct', 402], ['Hacker News', 286], ['Google', 230]],
    },
    {
      id: 'resume_downloaded', total: 342, uniq: 338, conv: '4.1%', delta: '+6%',
      pages: [['/about', 296], ['/', 46]],
      sources: [['Direct', 180], ['LinkedIn', 98], ['Google', 64]],
    },
    {
      id: 'theme_changed', total: 296, uniq: 244, conv: '3.0%', delta: '−2%',
      pages: [['/', 168], ['/blog', 84], ['/projects', 44]],
      sources: [['Direct', 202], ['Google', 94]],
    },
    {
      id: 'blog_post_shared', total: 158, uniq: 151, conv: '1.9%', delta: '+24%',
      pages: [['/blog/shipping-kawaii-ui', 112], ['/blog/why-i-left-big-analytics', 46]],
      sources: [['Direct', 88], ['Reddit', 70]],
    },
  ];

  eventSeries(): number[] {
    return this.visitors.map((v, i) => Math.round(v * 0.14 * (0.7 + ((i * 11) % 6) / 10)));
  }

  readonly selEventDevGeo: NameVal[] = [
    { name: 'Desktop', val: '64%' }, { name: 'Mobile', val: '36%' },
    { name: 'United States', val: '38%' }, { name: 'Germany', val: '13%' },
  ];

  // ── Goals ──────────────────────────────────────────────────────────
  readonly goals: GoalStats[] = [
    { name: 'Visited /projects', type: 'Page visit', conv: 2140, rate: '26.1%' },
    { name: 'Clicked GitHub', type: 'Event', conv: 918, rate: '11.2%' },
    { name: 'Downloaded résumé', type: 'Download', conv: 342, rate: '4.2%' },
    { name: 'Submitted contact form', type: 'Event', conv: 121, rate: '1.5%' },
  ];

  readonly goalTypes: [string, string, string][] = [
    ['page', 'Page visit', 'A visit reaches a specific page.'],
    ['event', 'Custom event', 'A mochi.event() call fires.'],
    ['outbound', 'Outbound link', 'A click leaves your site.'],
    ['download', 'File download', 'A file link is downloaded.'],
  ];

  readonly goalTargetByType: Record<string, [string, string]> = {
    page: ['Page path', '/projects'],
    event: ['Event name', 'github_link_clicked'],
    outbound: ['Link URL contains', 'github.com'],
    download: ['File path ends with', '.pdf'],
  };

  // ── Websites ───────────────────────────────────────────────────────
  readonly sites: SiteInfo[] = [
    { name: 'hazeliscoding', domain: 'hazeliscoding.com', views: '12,842', active: '3 active now', tone: 'success', status: 'Active', f: 1 },
    { name: 'kawaii-ui', domain: 'kawaii-ui.dev', views: '4,281', active: '1 active now', tone: 'success', status: 'Active', f: 0.6 },
    { name: 'portfolio', domain: 'portfolio.hazel.dev', views: '1,204', active: 'Quiet right now', tone: 'success', status: 'Active', f: 0.3 },
    { name: 'mochi-demo', domain: 'mochi-demo.dev', views: '—', active: 'No visits yet', tone: 'warning', status: 'Waiting for data', f: 0 },
  ];

  // ── Install snippets ───────────────────────────────────────────────
  readonly siteTag = '<script defer src="https://mochi.example/script.js" data-site="MC-7F3K2"></script>';

  readonly frameworks: Record<string, [string, string]> = {
    'HTML': ['index.html', '<!-- Just before </head> -->\n' + this.siteTag],
    'React': ['public/index.html', '<!-- Vite / CRA: add to the host page -->\n' + this.siteTag],
    'Angular': ['src/index.html', this.siteTag],
    'Vue': ['index.html', this.siteTag],
    'Next.js': ['app/layout.tsx', 'import Script from "next/script";\n\n// inside <body>\n<Script\n  src="https://mochi.example/script.js"\n  data-site="MC-7F3K2"\n  strategy="afterInteractive"\n/>'],
    'Astro': ['src/layouts/Layout.astro', '<script defer is:inline\n  src="https://mochi.example/script.js"\n  data-site="MC-7F3K2"></script>'],
  };

  readonly tzOptions = [
    '(UTC−08:00) Pacific Time', '(UTC−05:00) Eastern Time', '(UTC+00:00) London',
    '(UTC+01:00) Berlin', '(UTC+09:00) Tokyo',
  ];

  // ── Privacy center ─────────────────────────────────────────────────
  readonly privChecks: [string, string][] = [
    ['No cookies', 'Nothing is stored on your visitors’ devices.'],
    ['No fingerprinting', 'No canvas, font, or hardware probing.'],
    ['No cross-site tracking', 'What happens on your site stays on your site.'],
    ['No advertising profiles', 'Your data is never sold or shared.'],
    ['No session replay', 'Mochi never records what visitors do.'],
    ['No individual profiles', 'Visits are counted, people are not.'],
  ];

  readonly collectedItems: [string, string][] = [
    ['Page URL', 'the page that was viewed'], ['Referrer domain', 'where the visit came from'],
    ['Country', 'derived from IP, then the IP is discarded'], ['Device class', 'desktop, mobile, or tablet'],
    ['Browser & OS family', 'e.g. Firefox on Linux, no versions'], ['Custom events', 'names and counts you choose to send'],
  ];

  readonly notCollectedItems: [string, string][] = [
    ['IP addresses', 'used transiently, never written to disk'], ['Cookies or device IDs', 'no identifiers of any kind'],
    ['Names, emails, accounts', 'no personal information'], ['Precise location', 'nothing below country level by default'],
    ['Click paths per person', 'no individual timelines'], ['Anything cross-site', 'no shared profiles between websites'],
  ];

  readonly retentionOptions: [string, string][] = [
    ['30 days', 'Rolling month of daily aggregates.'], ['90 days', 'A quarter of history.'],
    ['1 year', 'Recommended for year-over-year trends.'], ['Unlimited aggregates', 'Daily totals forever, still nothing personal.'],
  ];

  readonly retention = signal('1 year');
}
