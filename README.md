# Mochi

Privacy-first web analytics — counts visits, not people.

A self-hostable alternative to Google Analytics that answers **who visits, from
where, on what, and what they did** without cookies, fingerprinting, or storing
anything personal. Visitors are day-scoped salted hashes; the salt is destroyed
daily, so cross-day tracking is cryptographically impossible, not just promised.

> Pre-1.0 and moving fast — see [ROADMAP.md](ROADMAP.md) for where things stand.
> A branded README with screenshots and a live instance lands with v1.0.

## How it works

1. A [1.9 KB snippet](backend/src/Mochi.Api/wwwroot/script.js) sends pageviews
   and custom events — no cookies, no storage, DNT/GPC respected.
2. The API scrubs each beacon at ingest: IP and user agent are reduced to
   country + browser/OS family and dropped, query strings stripped, referrers
   cut to domain + channel.
3. Raw events live 7 days, then a nightly job rolls them into daily aggregate
   tables — the only long-term data. Deleting a site deletes everything.

Decisions are documented as ADRs in [docs/adr/](docs/adr/): the visitor hash
(0001), API contracts (0002), storage and rollups (0003), auth (0004).

## Stack

- **Backend** — .NET 10 minimal API, DDD layering, EF Core + PostgreSQL
  (in-memory fallback for development), cookie-session auth.
- **Frontend** — Angular 22 (zoneless, signals) with the Trellis design system.
- **Tests** — xUnit + Testcontainers integration tests against real Postgres,
  Vitest, and Playwright e2e over the full stack.

## Development

```bash
# API on :5000 — no database needed (in-memory mode);
# the first-run setup code is printed in the log
cd backend/src/Mochi.Api && dotnet run --urls http://localhost:5000

# dashboard on :4200, proxying /api to the backend
cd frontend && npm install && npx ng serve --proxy-config proxy.conf.json

# tests
cd backend && dotnet test          # unit + integration (needs Docker)
cd frontend && npm run e2e         # playwright, boots both servers itself
```

To run against Postgres, set `ConnectionStrings__Mochi`; migrations apply on
startup. Pin the setup code with `MOCHI_SETUP_CODE` if you need it stable.
