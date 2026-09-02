import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  ApiCountRow,
  ApiDevices,
  ApiEventRow,
  ApiGeoRow,
  ApiGoal,
  ApiGoalStatsRow,
  ApiPageRow,
  ApiPrivacy,
  ApiRealtime,
  ApiSite,
  ApiSiteListItem,
  ApiSummary,
  ApiTimeseries,
} from './api-types';

/**
 * Dashboard data adapter. Fetches raw numbers from the .NET API and
 * formats them into the display shapes the pages consume.
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

export interface MetricCard {
  label: string;
  value: string;
  delta: string;
  dir: 'up' | 'down';
  tip: string;
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
  id: string;
  name: string;
  type: string;
  target: string;
  conv: number;
  rate: string;
}

export interface SiteInfo {
  id: string;
  name: string;
  domain: string;
  views: string;
  active: string;
  tone: 'success' | 'warning';
  status: string;
}

export interface TableColumn {
  key: string;
  label: string;
  numeric?: boolean;
}

export type FetchState = 'loading' | 'error' | 'ready';

interface StatsResource {
  hasValue(): boolean;
  error(): unknown;
}

export function fmt(n: number): string {
  return n.toLocaleString('en-US');
}

/** Seconds to "1m 46s". */
export function fmtDur(totalSec: number): string {
  const sec = Math.max(0, Math.round(totalSec));
  return `${Math.floor(sec / 60)}m ${String(sec % 60).padStart(2, '0')}s`;
}

const MINUS = '−';

function signed(text: string, diff: number): string {
  return (diff > 0 ? '+' : MINUS) + text;
}

function dirOf(diff: number): 'up' | 'down' {
  return diff < 0 ? 'down' : 'up';
}

/** Percent change vs the compare period, e.g. "+12.4%". Empty when not computable. */
function pctDelta(cur: number, prev: number | undefined): string {
  if (prev === undefined || prev <= 0 || cur === prev) return '';
  const d = ((cur - prev) / prev) * 100;
  return signed(Math.abs(d).toFixed(1) + '%', d);
}

function isoDay(d: Date): string {
  return d.toISOString().slice(0, 10);
}

function daysAgo(n: number): Date {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - n);
  return d;
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

function shortDate(iso: string): string {
  const [, m, day] = iso.split('-').map(Number);
  return `${MONTHS[m - 1]} ${day}`;
}

/** "/blog/shipping-kawaii-ui" to "Shipping kawaii ui"; "/" is Home. */
function pageTitle(path: string): string {
  if (path === '/') return 'Home';
  const seg = path.split('/').filter(Boolean).pop() ?? path;
  const words = seg.replace(/[-_]+/g, ' ');
  return words.charAt(0).toUpperCase() + words.slice(1);
}

function toBarRows(rows: ApiCountRow[]): BarRow[] {
  return rows.map(r => ({ name: r.name, val: fmt(r.count), pct: Math.round(r.pct) }));
}

/** Bar fill relative to the largest row; rows arrive sorted descending. */
function relBarRows(rows: ApiCountRow[]): BarRow[] {
  const max = rows[0]?.count ?? 0;
  return rows.map(r => ({ name: r.name, val: String(r.count), pct: max ? Math.round((r.count / max) * 100) : 0 }));
}

const DEVICE_COLORS: Record<string, string> = {
  Desktop: 'var(--color-accent)',
  Mobile: 'var(--teal-300)',
  Tablet: 'var(--color-border)',
};

const METRIC_TIPS: Record<string, string> = {
  Visitors: 'Unique visits in the period. A visit is a browsing session, not a persistent person.',
  Pageviews: 'Total pages loaded.',
  'Views per visitor': 'Pageviews divided by visitors.',
  'Bounce rate': 'Visits that left after a single page. Lower is usually better.',
  'Avg visit duration': 'Median time between first and last pageview of a visit.',
};

@Injectable({ providedIn: 'root' })
export class AnalyticsDataService {
  private readonly http = inject(HttpClient);

  // Global header state
  readonly siteId = signal<string | null>(null);
  readonly range = signal('Last 30 days');
  readonly compare = signal('vs previous period');

  readonly rangeOptions = ['Last 7 days', 'Last 30 days', 'Last 90 days', 'Year to date'];
  readonly compareOptions = ['vs previous period', 'vs same period last year', 'No comparison'];

  /** Inclusive UTC date window plus compare mode, derived from the header selectors. */
  private readonly period = computed(() => {
    const to = isoDay(new Date());
    const range = this.range();
    let from: string;
    if (range === 'Last 7 days') from = isoDay(daysAgo(6));
    else if (range === 'Last 90 days') from = isoDay(daysAgo(89));
    else if (range === 'Year to date') from = to.slice(0, 4) + '-01-01';
    else from = isoDay(daysAgo(29));
    const cmp = this.compare();
    const compare = cmp === 'vs same period last year' ? 'year' : cmp === 'No comparison' ? 'none' : 'previous';
    return { from, to, compare };
  });

  readonly rangeLabel = computed(() => {
    const { from, to } = this.period();
    return `${shortDate(from)} – ${shortDate(to)}, ${to.slice(0, 4)}`;
  });

  /** Evenly spaced date labels for chart x axes. */
  axisLabels(count: number): string[] {
    const { from, to } = this.period();
    const start = new Date(from + 'T00:00:00Z').getTime();
    const end = new Date(to + 'T00:00:00Z').getTime();
    if (count < 2) return [shortDate(to)];
    return Array.from({ length: count }, (_, i) =>
      shortDate(isoDay(new Date(start + ((end - start) * i) / (count - 1)))),
    );
  }

  private statsUrl(seg: string, opts?: { compare?: boolean; extra?: string }): string | undefined {
    const id = this.siteId();
    if (!id) return undefined;
    const { from, to, compare } = this.period();
    let url = `/api/sites/${id}/stats/${seg}?from=${from}&to=${to}`;
    if (opts?.compare && compare !== 'none') url += `&compare=${compare}`;
    return url + (opts?.extra ?? '');
  }

  // Resources; every one refetches when site, range or compare changes.
  readonly sitesRes = httpResource<ApiSiteListItem[]>(() => '/api/sites');
  readonly summaryRes = httpResource<ApiSummary>(() => this.statsUrl('summary', { compare: true }));
  readonly chartMetric = signal<'visitors' | 'pageviews' | 'sessions'>('visitors');
  readonly timeseriesRes = httpResource<ApiTimeseries>(() =>
    this.statsUrl('timeseries', { compare: true, extra: `&metric=${this.chartMetric()}` }),
  );
  readonly pagesRes = httpResource<ApiPageRow[]>(() => this.statsUrl('pages'));
  readonly channelsRes = httpResource<ApiCountRow[]>(() => this.statsUrl('sources', { extra: '&group=channels' }));
  readonly referrersRes = httpResource<ApiCountRow[]>(() => this.statsUrl('sources', { extra: '&group=referrers' }));
  readonly sourceGroup = signal('referrers');
  readonly sourceGroupRes = httpResource<ApiCountRow[]>(() =>
    this.statsUrl('sources', { extra: `&group=${this.sourceGroup()}` }),
  );
  readonly geoRes = httpResource<ApiGeoRow[]>(() => this.statsUrl('geo'));
  readonly devicesRes = httpResource<ApiDevices>(() => this.statsUrl('devices'));
  readonly eventsRes = httpResource<ApiEventRow[]>(() => this.statsUrl('events'));
  readonly realtimeRes = httpResource<ApiRealtime>(() => {
    const id = this.siteId();
    return id ? `/api/sites/${id}/stats/realtime` : undefined;
  });
  readonly goalsRes = httpResource<ApiGoal[]>(() => {
    const id = this.siteId();
    return id ? `/api/sites/${id}/goals` : undefined;
  });
  readonly goalStatsRes = httpResource<ApiGoalStatsRow[]>(() => {
    const id = this.siteId();
    if (!id) return undefined;
    const { from, to } = this.period();
    return `/api/sites/${id}/goals/stats?from=${from}&to=${to}`;
  });
  readonly privacyRes = httpResource<ApiPrivacy>(() => {
    const id = this.siteId();
    return id ? `/api/sites/${id}/privacy` : undefined;
  });

  constructor() {
    // Select the first site once the list arrives, or after the current one is deleted.
    effect(() => {
      const list = this.sitesRes.value();
      if (!list?.length) return;
      if (!list.some(s => s.site.id === this.siteId())) this.siteId.set(list[0].site.id);
    });
  }

  /** 'error' beats 'loading'; 'ready' only when every resource has a value. */
  stateOf(...resources: StatsResource[]): FetchState {
    if (resources.some(r => r.error() !== undefined)) return 'error';
    if (resources.some(r => !r.hasValue())) return 'loading';
    return 'ready';
  }

  // Sites
  readonly siteOptions = computed(() => (this.sitesRes.value() ?? []).map(s => ({ id: s.site.id, domain: s.site.domain })));
  private readonly selectedEntry = computed(
    () => (this.sitesRes.value() ?? []).find(s => s.site.id === this.siteId()) ?? null,
  );
  readonly site = computed(() => this.selectedEntry()?.site.domain ?? '');
  readonly currentSite = computed<ApiSite | null>(() => this.selectedEntry()?.site ?? null);
  readonly snippet = computed(() => this.currentSite()?.snippet ?? '');
  readonly hasData = computed(() => (this.summaryRes.value()?.current.pageviews ?? 0) > 0);

  readonly sites = computed<SiteInfo[]>(() =>
    (this.sitesRes.value() ?? []).map(s => ({
      id: s.site.id,
      name: s.site.name,
      domain: s.site.domain,
      views: fmt(s.viewsLast30d),
      active:
        s.activeNow > 0
          ? `${s.activeNow} active now`
          : s.status === 'active'
            ? 'Quiet right now'
            : 'No visits yet',
      tone: s.status === 'active' ? 'success' : 'warning',
      status: s.status === 'active' ? 'Active' : 'Waiting for data',
    })),
  );

  // Overview
  readonly metrics = computed<MetricCard[]>(() => {
    const s = this.summaryRes.value();
    if (!s) return [];
    const cur = s.current;
    const cmp = s.compare;
    const vpvDiff = cmp ? cur.viewsPerVisitor - cmp.viewsPerVisitor : 0;
    const bounceDiff = cmp ? cur.bounceRatePct - cmp.bounceRatePct : 0;
    const durDiff = cmp ? Math.round(cur.avgDurationSec - cmp.avgDurationSec) : 0;
    return [
      { label: 'Visitors', value: fmt(cur.visitors), delta: pctDelta(cur.visitors, cmp?.visitors), dir: dirOf(cur.visitors - (cmp?.visitors ?? 0)), tip: METRIC_TIPS['Visitors'] },
      { label: 'Pageviews', value: fmt(cur.pageviews), delta: pctDelta(cur.pageviews, cmp?.pageviews), dir: dirOf(cur.pageviews - (cmp?.pageviews ?? 0)), tip: METRIC_TIPS['Pageviews'] },
      { label: 'Views per visitor', value: cur.viewsPerVisitor.toFixed(1), delta: cmp && Math.abs(vpvDiff) >= 0.05 ? signed(Math.abs(vpvDiff).toFixed(1), vpvDiff) : '', dir: dirOf(vpvDiff), tip: METRIC_TIPS['Views per visitor'] },
      { label: 'Bounce rate', value: Math.round(cur.bounceRatePct) + '%', delta: cmp && Math.abs(bounceDiff) >= 0.05 ? signed(Math.abs(bounceDiff).toFixed(1) + ' pt', bounceDiff) : '', dir: dirOf(bounceDiff), tip: METRIC_TIPS['Bounce rate'] },
      { label: 'Avg visit duration', value: fmtDur(cur.avgDurationSec), delta: cmp && durDiff !== 0 ? signed(Math.abs(durDiff) + 's', durDiff) : '', dir: dirOf(durDiff), tip: METRIC_TIPS['Avg visit duration'] },
    ];
  });

  readonly series = computed(() => (this.timeseriesRes.value()?.points ?? []).map(p => p.value));
  readonly prevSeries = computed(() => (this.timeseriesRes.value()?.compare ?? []).map(p => p.value));

  readonly channelRows = computed<BarRow[]>(() => toBarRows(this.channelsRes.value() ?? []));
  readonly topSourceRows = computed<NameVal[]>(() =>
    (this.referrersRes.value() ?? []).slice(0, 5).map(r => ({ name: r.name, val: fmt(r.count) })),
  );
  readonly browserTop = computed(() =>
    (this.devicesRes.value()?.browsers ?? []).slice(0, 3).map(r => ({ name: r.name, pct: Math.round(r.pct) })),
  );
  readonly osTop = computed(() =>
    (this.devicesRes.value()?.os ?? []).slice(0, 3).map(r => ({ name: r.name, pct: Math.round(r.pct) })),
  );

  // Pages
  readonly pages = computed<PageStats[]>(() =>
    (this.pagesRes.value() ?? []).map(p => ({
      id: p.path,
      title: pageTitle(p.path),
      v: p.visitors,
      pv: p.pageviews,
      bounce: Math.round(p.bouncePct),
      dur: fmtDur(p.avgDurationSec),
      entry: p.entries,
      exit: p.exits,
    })),
  );

  // Geography
  readonly geoRows = computed(() =>
    (this.geoRes.value() ?? []).map(g => ({ name: g.name, val: fmt(g.visitors), pct: Math.round(g.pct) })),
  );

  /** Stylized world-map dot grid: per row, [startCol, endCol] land ranges. Decorative. */
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

  /** Highlight regions: [rowStart, rowEnd, colStart, colEnd, intensity]. Decorative. */
  readonly mapHighlights: [number, number, number, number, number][] = [
    [5, 7, 4, 12, 1], [2, 4, 4, 12, 0.5], [3, 3, 28, 29, 0.8], [4, 4, 31, 32, 0.85],
    [4, 4, 30, 30, 0.5], [5, 5, 29, 30, 0.45], [6, 7, 52, 54, 0.75], [17, 19, 51, 55, 0.5],
  ];

  // Devices
  readonly deviceClasses = computed(() => {
    const rows = this.devicesRes.value()?.classes ?? [];
    const order = ['Desktop', 'Mobile', 'Tablet'];
    return order.map(name => {
      const r = rows.find(x => x.name === name);
      return { name, pct: r ? Math.round(r.pct) : 0, val: fmt(r?.count ?? 0), color: DEVICE_COLORS[name] };
    });
  });
  readonly browserRows = computed<BarRow[]>(() => toBarRows(this.devicesRes.value()?.browsers ?? []));
  readonly osRows = computed<BarRow[]>(() => toBarRows(this.devicesRes.value()?.os ?? []));

  // Events
  readonly events = computed<EventStats[]>(() =>
    (this.eventsRes.value() ?? []).map(e => ({
      id: e.name,
      total: e.total,
      uniq: e.uniqueVisitors,
      conv: e.convPct.toFixed(1) + '%',
      // No compare data for events yet; empty delta hides the badge.
      delta: '',
      pages: e.pages.map(p => [p.name, p.count] as [string, number]),
      sources: e.sources.map(s => [s.name, s.count] as [string, number]),
    })),
  );

  // Realtime
  readonly rt = computed(() => {
    const r = this.realtimeRes.value();
    if (!r) return null;
    const d = r.devices;
    const devTotal = d.desktop + d.mobile + d.tablet;
    return {
      active: r.activeVisitors,
      vals: r.pageviewsPerMinute,
      maxVal: Math.max(1, ...r.pageviewsPerMinute),
      pages: relBarRows(r.pages),
      sources: relBarRows(r.sources),
      countries: r.countries.map(c => ({ name: c.name, val: String(c.count) })),
      devices: d,
      devicePct: {
        desktop: devTotal ? Math.round((d.desktop / devTotal) * 100) : 0,
        mobile: devTotal ? Math.round((d.mobile / devTotal) * 100) : 0,
      },
    };
  });

  // Site management
  createSite(body: { name: string; domain: string; timezone: string }): Observable<ApiSite> {
    return this.http.post<ApiSite>('/api/sites', body);
  }

  /**
   * Single update path for site settings; Settings and the Privacy center both
   * go through here. Refreshes the site list and the privacy summary so the
   * two pages never disagree on retention.
   */
  updateSite(id: string, body: { name?: string; timezone?: string; retention?: string }): Observable<ApiSite> {
    return this.http.put<ApiSite>(`/api/sites/${id}`, body).pipe(
      tap(() => {
        this.sitesRes.reload();
        this.privacyRes.reload();
      }),
    );
  }

  /** Same-origin URL for the full CSV export zip; the session cookie authorizes it. */
  exportUrl(id: string): string {
    return `/api/sites/${id}/export`;
  }

  deleteSite(id: string): Observable<void> {
    return this.http.delete<void>(`/api/sites/${id}`);
  }

  // Install snippets
  frameworksFor(snippet: string): Record<string, [string, string]> {
    const siteId = /data-site="([^"]+)"/.exec(snippet)?.[1] ?? '';
    const src = /src="([^"]+)"/.exec(snippet)?.[1] ?? '/script.js';
    return {
      'HTML': ['index.html', '<!-- Just before </head> -->\n' + snippet],
      'React': ['public/index.html', '<!-- Vite / CRA: add to the host page -->\n' + snippet],
      'Angular': ['src/index.html', snippet],
      'Vue': ['index.html', snippet],
      'Next.js': ['app/layout.tsx', `import Script from "next/script";\n\n// inside <body>\n<Script\n  src="${src}"\n  data-site="${siteId}"\n  strategy="afterInteractive"\n/>`],
      'Astro': ['src/layouts/Layout.astro', `<script defer is:inline\n  src="${src}"\n  data-site="${siteId}"></script>`],
    };
  }

  /** [display label, IANA id] pairs; the API stores the IANA id. */
  readonly tzOptions: [string, string][] = [
    ['(UTC−08:00) Pacific Time', 'America/Los_Angeles'],
    ['(UTC−05:00) Eastern Time', 'America/New_York'],
    ['(UTC+00:00) London', 'Europe/London'],
    ['(UTC+01:00) Berlin', 'Europe/Berlin'],
    ['(UTC+09:00) Tokyo', 'Asia/Tokyo'],
  ];

  /** [wire value, display label, description] for the retention setting. */
  readonly retentionChoices: [string, string, string][] = [
    ['30d', '30 days', 'Rolling month of daily aggregates.'],
    ['90d', '90 days', 'A quarter of history.'],
    ['1y', '1 year', 'Recommended for year-over-year trends.'],
    ['unlimited', 'Unlimited aggregates', 'Daily totals forever, still nothing personal.'],
  ];

  retentionLabel(wire: string): string {
    return this.retentionChoices.find(r => r[0] === wire)?.[1] ?? wire;
  }

  // Goals: definitions joined with conversion stats for the selected period.
  readonly goals = computed<GoalStats[]>(() => {
    const stats = new Map((this.goalStatsRes.value() ?? []).map(s => [s.id, s]));
    return (this.goalsRes.value() ?? []).map(g => {
      const s = stats.get(g.id);
      return {
        id: g.id,
        name: g.name,
        type: this.goalTypes.find(t => t[0] === g.type)?.[1] ?? g.type,
        target: g.target,
        conv: s?.conversions ?? 0,
        rate: (s?.ratePct ?? 0).toFixed(1) + '%',
      };
    });
  });

  createGoal(siteId: string, body: { name: string; type: string; target: string }): Observable<ApiGoal> {
    return this.http.post<ApiGoal>(`/api/sites/${siteId}/goals`, body);
  }

  deleteGoal(siteId: string, goalId: string): Observable<void> {
    return this.http.delete<void>(`/api/sites/${siteId}/goals/${goalId}`);
  }

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

  // Privacy center copy
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
}
