# Architecture Decision Records

Decisions that shape the Mochi backend, recorded before v0.2 implementation
starts. Format: numbered files, `Status / Context / Decision / Consequences`,
with honest open questions at the end of each. Supersede, don't edit history:
a reversed decision gets a new ADR that links back.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-privacy-preserving-visitor-sessions.md) | Privacy-preserving visitor and session model | Proposed |
| [0002](0002-api-contracts.md) | API contracts — ingestion, queries, site management | Proposed |
| [0003](0003-storage-and-aggregation.md) | Storage, aggregation, and retention | Proposed |

Reading order matters: 0001 defines the visitor hash that 0002's collect flow
computes and 0003's schema stores.
