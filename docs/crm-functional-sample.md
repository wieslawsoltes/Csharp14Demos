# Functional CRM Sample – Design Overview

## Objectives
- Showcase a realistic “mini CRM” that exercises the entire `FunctionalExtensions` surface area while using modern C# 14 syntax and patterns.
- Demonstrate how functional building blocks compose with Avalonia UI, SQLite persistence, HTTP effects, file I/O, and background processing.
- Provide a foundation developers can extend: the sample ships with domain models, repositories, sync workflows, and instrumentation wired up end-to-end.

## High-level Architecture
- **UI (Avalonia)**: Fluent-themed desktop shell with MVU-ish view models. Commands are modelled as `TaskResult<Unit>` pipelines to keep failure handling declarative. UI state (filters, selections) flows through `State<TState, TValue>` combinators so view models stay stateless.
- **Application Layer**: Records domain workflows (`CrmWorkflows`) that orchestrate repositories, validation, and side-effects. Each workflow is exposed as `ReaderTaskResult<AppEnvironment, TResult>` so it can be dependency-injected or tested with fake environments.
- **Domain & Storage**:
  - Entities use `Lens` optics for targeted updates (e.g., updating a contact’s preferred channel).
  - Validation gates rely on `Validation<T>` combined with `SequenceExtensions.Validation` helpers for rich error reporting.
  - SQLite persistence uses `DbConnectionStateTaskResults` to guarantee transactions and emit `Writer` logs for auditing.
  - File attachments leverage `TaskIO` + `Try` to guard against filesystem errors while still exposing deterministic `Result<T>` shapes to callers.
- **Integration**:
  - Background enrichment fetches data from `https://jsonplaceholder.typicode.com/users` via `HttpClientTaskResults`, writing progress updates into a `ChannelWriter` fed to the UI.
  - Export/import flows use `IO<T>` to delay work until the user confirms, with `TaskResultDoBuilder` giving do-notation style ergonomics while orchestrating `TaskResult<T>` operations.

## Data Flow
1. **User intent** triggers an Avalonia command.
2. The command calls a `ReaderTaskResult` workflow, binding dependencies (database, file storage, HTTP, clock, logger).
3. The workflow constructs one or more `StateTaskResult<DbTransactionState, TValue>` pipelines, ensuring every SQLite mutation is transactional.
4. Each step produces domain events accumulated via `Writer<TLog, TValue>`; the log stream feeds both diagnostics and UI toasts.
5. UI observes materialized `Result<T>` values and `Option<T>` selections, reacting with `Sequence` operators (sorting, grouping) to render dashboards.

## FunctionalExtensions Coverage Map
| Module | Sample Usage |
| --- | --- |
| `Option` | Optional secondary contacts, avatar presence checks, `Functor.Option.FMap`, `Applicative.Option.Apply`, `Monad.Option.Bind` patterns. |
| `Result`/`TaskResult` | Every command returns `TaskResult<TResult>`; Program bootstrapping awaits `TaskResultDoBuilder` workflows. |
| `Validation` | Customer draft validation and CSV import sanitization combine multiple failures into a single payload. |
| `Try` | Wraps risky file attachment moves and JSON parsing. |
| `Reader`/`ReaderTaskResult` | Dependency injection for workflows and effects (database, HTTP client, telemetry). |
| `State`/`StateTaskResult` | UI filter state mutations + transactional DB pipelines using `DbTransactionState`. |
| `Writer` | Audit trail and toast notifications aggregated from commands. |
| `Sequence` | Bulk import/export transformations, streaming HTTP payload handling, and grouping logic. |
| `TaskIO` | Async file copy/delete operations surfaced as `TaskResult<Unit>`. |
| `IO` | Lazy export pipelines; only executed after confirmation dialogs. |
| `Continuation` | Implements an undo stack with `Continuation.Cont` to short-circuit replay. |
| `Optics.Lens` | Fine-grained updates to nested immutable records (e.g., updating a contact’s address). |
| `Numerics` | Uses `Rational` for lead scoring normalization and `ComplexExtensions` for trend calculations inside dashboards. |
| `Patterns` | Active patterns simplify span-based CSV parsing. |
| `Effects` | `DbConnectionStateTaskResults`, `HttpClientTaskResults`, and `ChannelWriterTaskResults` wire up the imperative edge cases. |
| `TypeClasses` | Demonstrates functor/applicative/monad combinators for `Option`, `Task`, and `IEnumerable`. |
| `Computation.TaskResultDoBuilder` | Provides succinct “do-notation” for orchestrating async commands. |

## Persistence & Files
- SQLite database lives under `%LocalAppData%/FunctionalCRM/crm.db` (or the Unix equivalent).
- Attachments stored alongside the DB in `attachments/`. Metadata is persisted in SQLite while file contents remain on disk.
- Each write operation streams changes into a JSONL audit file via `TaskIO` and `Writer`, ensuring file manipulation is showcased.

## UI/UX Highlights
- Customers grid with search/filter (state monad) and detail panel with validation badges.
- Activities timeline uses `Sequence` combinators to merge customer communications, tasks, and background sync events.
- Attachment manager allows drag/drop (simulated) and uses `Try`/`TaskIO` internally.
- Export/import wizard demonstrates `IO`, `TaskResultDoBuilder`, and file manipulation.
- Toolbar exposes create/read/update/delete actions (New, Load, Save, Delete) routed through `ReaderTaskResult` workflows so the sample exercises the full CRUD surface end-to-end.

## Diagnostics
- Bounded channel publishes toast notifications; UI subscribes and displays them via Avalonia `ItemsRepeater`.
- Optional verbose logging writes to `logs/telemetry.jsonl`.

This document is intentionally exhaustive so future work can trace which part of the sample triggers each `FunctionalExtensions` concept.
