# 🍡 Mochi — Design Documentation

---

[← Previous](04-data-model.md) | [Next →](06-dedupe-and-bot-filtering.md)

---

## 05-ingestion-and-hashing

### Tracking Payload (Client → Server)

```json
{
  "trackingKey": "pk_live_xxxxx",
  "path": "/projects/tiktok-clone",
  "referrer": "https://github.com/",
  "timestamp": "2026-01-16T18:42:11Z"
}
```

The payload is intentionally minimal. The client does not attempt to identify users or manage sessions.

---

### Server-Side Enrichment

On ingestion, the server augments the payload with contextual information such as IP address and User-Agent. These values are never stored in raw form.

---

### Visitor Hashing Strategy

```text
visitor_hash = SHA256(
  ip + "|" + userAgent + "|" + dailySalt
)
```

```text
dailySalt = SHA256(YYYY-MM-DD + serverSecret)
```

This approach provides approximate uniqueness within a single day while explicitly preventing long-term tracking.

---

[← Previous](04-data-model.md) | [Next →](06-dedupe-and-bot-filtering.md)
