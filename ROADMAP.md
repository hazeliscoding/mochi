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

- [x] Authentication, single user (ADR 0004: server-side cookie sessions,
      XSRF double-submit, PasswordHasher V3, first-run setup code printed to
      the server log; login/setup pages, 401 redirect, header logout)
- [x] Per-site access control (users/sites/site_users membership; anonymous
      401, non-member 404; collect and script.js stay public)
- [x] Onboarding flow: add site → snippet shown → verify checks site status
- [x] Goals CRUD + stats endpoint (`GET …/goals/stats`, conversions computed
      at query time so new goals show history immediately)
- [x] Wire the Goals page to the goals endpoints (create dialog, delete with
      confirm, range-reactive conversion stats)
- [x] ADR 0004: authentication and access (cookie sessions, first-run setup
      code, site membership model)
- [ ] Teams: invitations, editor/viewer roles (membership model is ready;
      needs email or invite-link infrastructure, candidate to defer past 1.0)

## v0.6 — Privacy center, for real ✅

> You can now: point at actual behavior behind every privacy promise.

- [x] Configurable data retention with automatic purge (nightly job purges
      rollups past the site's setting and raw events past 7 days; unit-tested)
- [x] Data export: zip with a CSV per daily aggregate table plus goals, from
      the Privacy Center's Export button
- [x] Privacy page backed by actual behavior: a live "what Mochi holds right
      now" card (raw events held, oldest aggregate, retention), retention
      radios that persist, and the untrue "privacy thresholds" claim replaced
      with honest day-scoped-hash copy

## v0.7 — Hardening ✅

> You can now: trust a green build to mean the product works.

- [x] Unit tests for aggregation logic (sessionizer, rollup/retention purge)
      and component tests for UI (accumulated across v0.2 to v0.6)
- [x] E2E via Playwright over the full stack, 15 tests (exceeds the headless
      Chrome smoke this called for)
- [x] CI pipeline on push and PR: backend tests with Testcontainers Postgres,
      frontend prettier check + build + unit tests, full e2e with failure
      traces uploaded; first run green

## v1.0 — Self-hostable release, deployed for real

> You can now: run Mochi yourself with one command, and visit the live
> instance tracking hazeliscoding.com.

- [x] Deployment story: multi-stage Dockerfile (one image, the API serves the
      SPA and the snippet) + docker-compose with Postgres; verified end to end
- [x] Install & upgrade documentation (README: compose one-liner, env table,
      upgrade = pull + up, migrations apply on startup)
- [x] Branding pass: logo/icon (SVG + PNG favicons + apple-touch-icon), SEO
      meta and OpenGraph tags, light + dark screenshot grid incl. the Privacy
      Center, full README in the house style (`npm run shots` regenerates)
- [ ] Versioned releases with migration path for the database (tag + GitHub
      release once the live instance is up)
- [x] Live deployment on Railway: Dockerfile build, managed Postgres over the
      private network, HTTPS at mochi-production-94fe.up.railway.app,
      forwarded headers honored (real visitor IPs and Secure cookies)
- [ ] Dogfooding: install the snippet on hazeliscoding.dev / rozeangel.moe
      (after admin setup and the rate-limiting hardening below)
- [ ] Public-exposure prerequisites from the ADR open questions: collect
      rate limiting / Origin validation (ADR 0002), login rate limiting
      (ADR 0004), production `SnippetBaseUrl`, GeoLite2 database configured

---

*Scope within a version is loose; versions are ordered by dependency. v0.2–v0.4
are the critical path to a usable product — everything after is trust and polish.*
