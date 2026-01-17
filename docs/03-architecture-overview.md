# 🍡 Mochi — Design Documentation

---

[← Previous](02-scope-and-mvp.md) | [Next →](04-data-model.md)

---

## 03-architecture-overview

### High-Level Flow

```mermaid
flowchart LR
    A[Website / SPA] -->|POST /api/track| B[Ingestion API]
    B --> C[(page_view_events)]
    C --> D[Aggregation Worker]
    D --> E[(daily_site_metrics)]
    D --> F[(daily_page_metrics)]
    E --> G[Dashboard API]
    F --> G
    G --> H[Angular Dashboard]
```

---

### Architectural Rationale

The architecture reflects a clear separation of responsibilities:

* **Ingestion** is optimized for speed and resilience
* **Raw storage** captures events with minimal processing
* **Aggregation** converts raw data into stable, query-friendly metrics
* **The dashboard** reads only pre-aggregated data

This design avoids expensive ad-hoc queries and scales naturally with traffic volume. Eventual consistency is an acceptable and intentional tradeoff for analytics workloads.

---

[← Previous](02-scope-and-mvp.md) | [Next →](04-data-model.md)
