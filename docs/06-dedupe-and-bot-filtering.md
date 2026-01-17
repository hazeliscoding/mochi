# 🍡 Mochi — Design Documentation

---

[← Previous](05-ingestion-and-hashing.md) | [Next →](07-aggregation-worker.md)

---

## 06-dedupe-and-bot-filtering

### Deduplication Strategy

Duplicate events within a short window are filtered using a deterministic dedupe key.

```sql
CREATE TABLE ingestion_dedupe (
    site_id UUID NOT NULL,
    dedupe_key TEXT NOT NULL,
    occurred_minute TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (site_id, dedupe_key, occurred_minute)
);
```

This protects against accidental double-counting without introducing stateful client logic.

---

### Bot Filtering

Mochi applies conservative, server-side heuristics to reject clearly automated traffic. The intent is not to perfectly classify bots, but to keep analytics trends meaningful.

---

[← Previous](05-ingestion-and-hashing.md) | [Next →](07-aggregation-worker.md)
