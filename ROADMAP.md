# Mochi Roadmap

Mochi is a privacy-first web analytics platform: no cookies, no fingerprinting,
aggregates only. Each version below is a shippable milestone with a clear
"you can now…" outcome. Pre-1.0, minor versions may break APIs.

Architecture decisions live in [docs/adr/](docs/adr/): the daily-salted visitor
hash (0001), the API contracts (0002), and the storage/rollup model (0003).

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

## v0.2 — Data in ✅

> You can now: send a pageview to a running API and see it stored in Postgres,
> rolled up into daily aggregates.

- [x] Scaffold .NET API project (`backend/`, DDD: Domain / Application /
      Infrastructure / Api, tests wired up)
- [x] Event ingestion endpoint (`POST /api/collect`) — pageviews + custom
      events, scrubbed per ADR 0002, day-salted visitor hash per ADR 0001
- [x] Website CRUD (register site, generate site ID, snippet in response)
- [x] Postgres + EF Core (schema per ADR 0003, migrations, in-memory adapters
      remain the no-connection-string dev fallback)
- [x] Rollup/purge job (sessionizes closed days into `daily_*` tables at 00:05
      UTC, purges raw events after 7 days and rollups per site retention;
      manual rerun via `POST /api/admin/rollup/{date}`)
- [x] Real user-agent parser (uap-core); GeoIP via MaxMind, active once a
      GeoLite2-Country.mmdb path is configured (`Mochi:GeoIpDatabase`)
- [x] Integration tests: Testcontainers Postgres + real HTTP happy paths
      (register, collect, rollup, cascade delete)

## v0.3 — Tracking script ✅

> You can now: drop a snippet on any site and Mochi receives its traffic.

- [x] Tiny embeddable snippet served at `/script.js` (1.9 KB, no cookies, no
      storage; size and storage-API bans enforced by integration tests)
- [x] Pageview auto-tracking + SPA route-change detection (pushState/popstate)
- [x] Custom event API (`mochi('event', 'signup')`) with a pre-load stub queue
- [x] Respect DNT / Global Privacy Control
- [x] Verified end to end: cross-origin page in headless Chrome produced
      pageview, SPA pageview and custom event rows in Postgres

## v0.4 — Data out ✅

> You can now: open the dashboard and see real traffic instead of mock data.

- [x] Query endpoints (`/api/sites/{id}/stats/*`): summary, timeseries, pages,
      sources, geo, devices, events, realtime; closed days from rollups, today
      sessionized live so definitions agree; compare=previous|year
- [x] Replace `analytics-data.service.ts` constants with HTTP calls (service
      formats numbers into the existing display shapes; pages barely changed)
- [x] Date-range + comparison-period filters hitting real queries
- [x] Realtime page polling every 15 s
- [x] Loading / empty / error states for every page
- [x] Real snippet in the Add Website / Settings pages; Websites page shows
      live viewsLast30d / activeNow / status
- [x] Playwright e2e suite (`npm run e2e`): 9 happy-path tests boot API +
      frontend, seed traffic, verify all pages render real data
- Cards without backing endpoints were removed, not faked (per-page
  referrer/device/country, per-event sparklines, US regions); Goals page stays
  mock until the goals stats endpoint (moved to v0.5)

## v0.5 — Accounts & multi-site

> You can now: log in, add several sites, and only see your own.

- [ ] Authentication (single user first, then teams)
- [ ] Per-site access control
- [ ] Onboarding flow: add site → verify snippet installed → first data
- [ ] Goals stats endpoint (`GET …/goals/stats`) and wire the Goals page
      (deferred from v0.4)

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
