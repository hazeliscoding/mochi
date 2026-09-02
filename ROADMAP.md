# Mochi Roadmap

Mochi is a privacy-first web analytics platform: no cookies, no fingerprinting,
aggregates only. Each version below is a shippable milestone with a clear
"you can now…" outcome. Pre-1.0, minor versions may break APIs.

## v0.1 — UI prototype ✅ (current)

> You can now: click through the entire product on mock data.

- [x] Angular 22 app with 13 routed pages (Overview, Realtime, Pages, Sources,
      Geography, Devices, Events, Goals, Websites, Add Website, Privacy Center,
      Settings)
- [x] Trellis design system (`frontend/src/styles/trellis/`) with light/dark theming
- [x] UI component library (metric cards, data tables, sparklines, bar lists,
      tabs, dialogs, tags, status indicators, code blocks)
- [x] Mock data layer (`analytics-data.service.ts`) ported 1:1 from the approved
      design — deliberately shaped as the seam for the future backend

## v0.2 — Data in

> You can now: send a pageview to a running API and see it stored.

- [ ] Scaffold .NET API project (`backend/`)
- [ ] Event ingestion endpoint (`POST /api/collect`) — pageviews + custom events
- [ ] Storage & aggregation model (sessions derived without cookies: rotating
      salted hash of IP + UA, salt discarded daily)
- [ ] Website CRUD (register site, generate site ID)

## v0.3 — Tracking script

> You can now: drop a snippet on any site and Mochi receives its traffic.

- [ ] Tiny embeddable `mochi.js` snippet (< 2 KB, no cookies, no localStorage)
- [ ] Pageview auto-tracking + SPA route-change detection
- [ ] Custom event API (`mochi('event', 'signup')`)
- [ ] Respect DNT / Global Privacy Control
- [ ] Serve the real snippet in the Add Website / Settings pages

## v0.4 — Data out

> You can now: open the dashboard and see real traffic instead of mock data.

- [ ] Query endpoints matching the frontend's data shapes (`PageStats`,
      `BarRow`, `EventStats`, `GoalStats`, `SiteInfo`, …)
- [ ] Replace `analytics-data.service.ts` constants with HTTP calls
- [ ] Date-range + comparison-period filters hitting real queries
- [ ] Realtime page via polling or SSE
- [ ] Loading / empty / error states for every page

## v0.5 — Accounts & multi-site

> You can now: log in, add several sites, and only see your own.

- [ ] Authentication (single user first, then teams)
- [ ] Per-site access control
- [ ] Onboarding flow: add site → verify snippet installed → first data

## v0.6 — Privacy center, for real

> You can now: point at actual behavior behind every privacy promise.

- [ ] Configurable data retention with automatic purge
- [ ] Data export (site owner)
- [ ] Public privacy commitments page backed by actual behavior, not copy

## v0.7 — Hardening

> You can now: trust a green build to mean the product works.

- [ ] Unit tests for aggregation logic; component tests for UI (Vitest is wired
      up, only the default spec exists)
- [ ] E2E smoke via headless Chrome
- [ ] CI pipeline (build, test, lint)

## v1.0 — Self-hostable release

> You can now: run Mochi yourself with one command and keep it running.

- [ ] Deployment story (Docker compose: API + DB + static frontend)
- [ ] Versioned releases with migration path for the database
- [ ] Install & upgrade documentation

---

*Scope within a version is loose; versions are ordered by dependency. v0.2–v0.4
are the critical path to a usable product — everything after is trust and polish.*
