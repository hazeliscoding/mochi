# 🍡 Mochi — Design Documentation

---

[← Previous](07-aggregation-worker.md) | [Next →](09-frontend-ui.md)

---

## 08-api-contracts

The API surface is intentionally small and explicit. Each endpoint exists to support a specific dashboard capability.

---

### Create Site

```
POST /api/sites
```

---

### List Sites

```
GET /api/sites
```

---

### Track Page View

```
POST /api/track
```

Returns:

```
204 No Content
```

---

### Metrics Overview

```
GET /api/sites/{siteId}/metrics/overview?days=30
```

---

### Top Pages

```
GET /api/sites/{siteId}/metrics/top-pages?days=30&limit=10
```

---

[← Previous](07-aggregation-worker.md) | [Next →](09-frontend-ui.md)
