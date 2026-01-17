# 🍡 Mochi — Design Documentation

> **Mochi** is a self-hosted, privacy-first analytics platform designed to help developers understand how their personal websites are used — without resorting to invasive tracking techniques.
>
> This repository intentionally begins with **design-first documentation**. The goal is to clearly articulate intent, scope, tradeoffs, and system boundaries *before* any production code exists. This mirrors how I approach larger systems in professional environments: alignment and clarity first, implementation second.

---

## 📁 Document Index

* [01-vision-and-goals.md](01-vision-and-goals.md)
* [02-scope-and-mvp.md](02-scope-and-mvp.md)
* [03-architecture-overview.md](03-architecture-overview.md)
* [04-data-model.md](04-data-model.md)
* [05-ingestion-and-hashing.md](05-ingestion-and-hashing.md)
* [06-dedupe-and-bot-filtering.md](06-dedupe-and-bot-filtering.md)
* [07-aggregation-worker.md](07-aggregation-worker.md)
* [08-api-contracts.md](08-api-contracts.md)
* [09-frontend-ui.md](09-frontend-ui.md)

---

---

Previous ← | [Next →](02-scope-and-mvp.md)

---

## 01-vision-and-goals

### 🎯 Vision

Mochi exists to answer a very practical question:

> *“How are my personal websites actually being used?”*

The platform intentionally avoids the excesses of modern analytics tooling. Mochi is not concerned with identifying users, building behavioral profiles, or tracking individuals across time and properties. Instead, it focuses on **aggregate understanding** — trends, popularity, and general usage patterns.

This constraint is not accidental. It is a deliberate design choice that informs every layer of the system.

---

### Core Principles

* **Privacy by design**
  User privacy is not a feature or a configuration toggle. Mochi is designed such that invasive tracking is structurally impossible.

* **Statistical usefulness over precision**
  Analytics data is inherently approximate. Mochi embraces this reality rather than pretending to provide perfect certainty.

* **Fast ingestion, slow aggregation**
  The system prioritizes cheap, reliable writes at ingestion time and shifts complexity to background processing.

* **Self-hostable and transparent**
  Mochi is intended to be understood, audited, and run by its owner.

* **Intentionally scoped**
  The project is designed to be finished. Features are added only when they meaningfully serve the core goal.

---

Previous ← | [Next →](02-scope-and-mvp.md)
