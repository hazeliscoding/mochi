# Mochi Analytics — Roadmap

## v0.1 — Frontend prototype ✅

- [x] Import the Mochi Analytics design and the Trellis design system
- [x] Angular 22 workspace (standalone components, signals, zoneless, lazy routes)
- [x] Trellis tokens & component CSS as global styles; Angular ports of the DS primitives
- [x] All 12 screens: Overview, Realtime, Pages (+ detail), Sources, Geography, Devices,
      Events (+ detail), Goals, Websites, Add website wizard, Privacy center, Settings
- [x] Dark/light theme with persistence; responsive shell (sidebar / tab nav)
- [x] Mock data service mirroring the design's numbers (`core/analytics-data.service.ts`)

## v0.2 — .NET backend

- [ ] Scaffold ASP.NET Core API in `backend/` (minimal APIs, OpenAPI)
- [ ] Data model: sites, daily aggregates, pages, sources, geo, devices, events, goals
- [ ] EF Core + database (SQLite for dev, PostgreSQL for prod)
- [ ] Read endpoints matching each screen's data shapes
- [ ] Swap `AnalyticsDataService` mock internals for HTTP calls (shapes stay identical)
- [ ] Wire the loading / first-run / quiet states designed for Overview and Realtime
- [ ] Site CRUD: add-website wizard, settings, and delete flows hit the API

## v0.3 — Ingestion pipeline

- [ ] `script.js` tracking snippet (<1 KB, no cookies, no identifiers)
- [ ] Ingestion endpoint: derive country then discard IP; parse UA into class/family only
- [ ] Aggregation job producing daily rollups; nothing per-visitor is stored
- [ ] Custom events API (`mochi.event()`) counted in aggregate
- [ ] Privacy thresholds: group/hide segments too small to be anonymous
- [ ] Realtime feed (last 5 minutes) via SSE
- [ ] Bot filtering, excluded paths, query-parameter stripping per site settings

## v0.4 — Accounts & polish

- [ ] Authentication and per-user site ownership
- [ ] Goals engine (page visit / event / outbound / download) with real conversion rates
- [ ] CSV export and data deletion (retention enforcement)
- [ ] Date-range and comparison selectors driving real queries
- [ ] E2E tests (Playwright) and accessibility audit
- [ ] Deployment: CI, containerized frontend + API
