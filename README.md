# 🍡 Mochi Analytics

Privacy-first web analytics — Mochi counts visits, not people. No cookies, no fingerprinting,
no individual profiles.

Built from the [Mochi Analytics design](https://claude.ai/design/p/e8eb9652-558b-4336-a473-0543ed5bef86)
on the Trellis design system. See [ROADMAP.md](ROADMAP.md) for where this is headed.

## Structure

| Path | What it is |
| --- | --- |
| `frontend/` | Angular 22 app (standalone components, signals, zoneless) |
| `backend/` | Planned .NET API |
| `design-reference/` | Imported design source, kept for reference |

## Quick start

```sh
cd frontend
npm install
npm start        # http://localhost:4200
npm run build    # production build
npm test         # unit tests (vitest)
```

## Screens

- 📊 **Analytics** — Overview, Realtime, Pages (+ detail), Sources, Geography, Devices, Events (+ detail), Goals
- ⚙️ **Manage** — Websites, Add website wizard, Privacy center, Website settings

Dark theme is the default; the header toggle persists your choice.

## Notes

- All data is currently mocked in `frontend/src/app/core/analytics-data.service.ts` — the seam for
  the future .NET backend. Replace its internals with HTTP calls; the shapes stay the same.
- Trellis tokens/CSS live in `frontend/src/styles/trellis/`; reusable Angular ports of the design
  system primitives are in `frontend/src/app/ui/`.
- Icons are Lucide (ISC), inlined in `ui/icons.ts` — no runtime icon dependency.
