# ADR 0001: Privacy-preserving visitor and session model

**Status**: Proposed (2026-09-02)

## Context

Mochi's core promise (see the Privacy Center page and `ROADMAP.md`) is: no cookies,
no fingerprinting, no cross-site tracking, no individual profiles. The dashboard
still needs the metrics every analytics product reports — unique visitors, sessions,
bounce rate, visit duration, entry/exit pages — and all of them require grouping
pageviews that belong to the same visit.

Conventional analytics solves this with a client-side identifier (cookie or
localStorage ID). That is off the table: `mochi.js` must ship with no client-side
storage of any kind (v0.3 roadmap item). We need a server-side way to say "these
two pageviews are probably the same visit" without ever storing who the visitor is.

## Decision

Derive a transient visitor identity on the server at ingestion time:

```
visitor_hash = SHA-256(daily_salt || site_id || client_ip || user_agent)
```

- **`daily_salt`** is a random 256-bit value generated at 00:00 UTC each day. The
  previous day's salt is destroyed at rotation. It exists only in the API process
  (persisted to the DB solely so multiple instances / restarts within the same day
  agree, and deleted on rotation).
- **`client_ip` and `user_agent` are inputs to the hash only.** The raw IP is used
  transiently to derive the country (GeoIP lookup) and the hash, then discarded.
  Neither is ever written to disk. The UA is reduced to browser family, OS family,
  and device class (desktop/mobile/tablet) before storage — no versions.
- **`site_id`** is included so the same person visiting two Mochi-tracked sites
  produces two unrelated hashes: no cross-site linkage even inside our own DB.
- The stored `visitor_hash` is truncated to 64 bits — enough to group a day's
  traffic, useless as a long-term identifier.

**Sessions** are derived from the visitor hash plus an inactivity window: a session
is a run of events with the same `visitor_hash` where consecutive events are ≤ 30
minutes apart. Sessions are computed server-side (see ADR 0003 for where); the
browser is never told a session exists.

**Why the hash is not fingerprinting**: fingerprinting means building a durable
identifier from device characteristics. This hash is deliberately *not durable* —
the salt rotation guarantees that yesterday's hash cannot be recomputed or matched
against today's, even by us, even under subpoena, because the salt no longer exists.

## Consequences

Accepted tradeoffs — these are the price of the privacy model and should be
documented in user-facing docs, not hidden:

- **Visitor counts reset at the day boundary (UTC).** A person who visits Monday
  and Tuesday counts as two visitors. "Unique visitors" over a multi-day range is
  the *sum of daily uniques*, which overcounts real people. This matches Plausible
  and Fathom semantics; the Overview metric tooltip already says "a visit is a
  browsing session, not a persistent person."
- **No returning-visitor or retention metrics.** New vs. returning, cohorts, and
  cross-day funnels are impossible by design. If a feature request needs them,
  the answer is no (or this ADR gets superseded).
- **A session spanning midnight UTC splits in two** (the hash changes under it).
  Rare and acceptable; do not special-case it.
- **Shared IPs (offices, CGNAT, VPNs) collapse distinct people** with identical
  browser/OS into one visitor; a UA change (browser update mid-day) splits one
  person into two. Counts are honest approximations, not census data.
- **Realtime "active visitors" works naturally**: distinct `visitor_hash` values
  seen in the last 5 minutes — no identifier needed beyond what ingestion already
  computes.
- **Salt rotation must be reliable.** A missed rotation extends linkability past
  24 h; rotation is a scheduled job with the current date stamped next to the salt
  so a late-starting instance can detect a stale salt and rotate immediately.

## Open questions

- **Rotation timezone**: UTC is simplest and is the decision above, but sites have
  a configured timezone (Settings page). Rotating per-site at local midnight would
  align the "visitors reset" boundary with the dashboard's day buckets at the cost
  of one salt per timezone. Revisit if UTC-boundary artifacts confuse users.
- **Hash truncation width**: 64 bits is comfortable for single-site daily volumes;
  confirm collision math if a site exceeds ~10 M daily visitors (not a v0.x concern).
