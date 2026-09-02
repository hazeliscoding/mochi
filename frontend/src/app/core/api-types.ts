/** Wire shapes of the .NET query API (ADR 0002). Numbers only; formatting happens in AnalyticsDataService. */

export interface ApiSite {
  id: string;
  name: string;
  domain: string;
  timezone: string;
  retention: string;
  snippet: string;
}

export interface ApiSiteListItem {
  site: ApiSite;
  viewsLast30d: number;
  activeNow: number;
  status: 'active' | 'waiting';
}

export interface ApiSummaryStats {
  visitors: number;
  pageviews: number;
  viewsPerVisitor: number;
  bounceRatePct: number;
  avgDurationSec: number;
}

export interface ApiSummary {
  current: ApiSummaryStats;
  compare: ApiSummaryStats | null;
}

export interface ApiPoint {
  date: string;
  value: number;
}

export interface ApiTimeseries {
  points: ApiPoint[];
  compare: ApiPoint[] | null;
}

export interface ApiPageRow {
  path: string;
  visitors: number;
  pageviews: number;
  bouncePct: number;
  avgDurationSec: number;
  entries: number;
  exits: number;
}

export interface ApiCountRow {
  name: string;
  count: number;
  pct: number;
}

export interface ApiGeoRow {
  code: string;
  name: string;
  visitors: number;
  pct: number;
}

export interface ApiDevices {
  classes: ApiCountRow[];
  browsers: ApiCountRow[];
  os: ApiCountRow[];
}

export interface ApiEventRow {
  name: string;
  total: number;
  uniqueVisitors: number;
  convPct: number;
  pages: ApiCountRow[];
  sources: ApiCountRow[];
}

export interface ApiAuthStatus {
  needsSetup: boolean;
  authenticated: boolean;
  email: string | null;
  isAdmin: boolean | null;
}

export interface ApiGoal {
  id: string;
  name: string;
  type: string;
  target: string;
}

export interface ApiGoalStatsRow {
  id: string;
  name: string;
  type: string;
  target: string;
  conversions: number;
  ratePct: number;
}

export interface ApiRealtime {
  activeVisitors: number;
  pageviewsPerMinute: number[];
  pages: ApiCountRow[];
  sources: ApiCountRow[];
  countries: ApiCountRow[];
  devices: { desktop: number; mobile: number; tablet: number };
}
