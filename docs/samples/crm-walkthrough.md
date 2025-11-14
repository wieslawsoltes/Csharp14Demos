# Functional CRM Walkthrough

The Avalonia-based CRM sample under `FunctionalExtensions.CrmSample/` demonstrates how every major module in FunctionalExtensions works together inside a realistic desktop workflow. Use this guide to run the app, explore core scenarios, and trace each screen back to the functional patterns it exercises.

## 1. Prerequisites
- .NET 10 preview SDK (matching `global.json`).
- Avalonia preview runtime dependencies (GTK3 on Linux).
- An HTTP connection (the enrichment job hits `https://jsonplaceholder.typicode.com/users`).

## 2. Running the Sample
```bash
dotnet run --project FunctionalExtensions.CrmSample/FunctionalExtensions.CrmSample.csproj
```

CLI switches:
- `-- --seed` – Rebuilds the SQLite database and attachment folder from fixtures.
- `-- --log` – Enables verbose audit logging under `%LocalAppData%/FunctionalCRM/logs/telemetry.jsonl`.

Data paths:
- Database: `%LocalAppData%/FunctionalCRM/crm.db` on Windows, `$HOME/.local/share/FunctionalCRM/crm.db` on Linux/macOS.
- Attachments: sibling `attachments/` directory per profile.

## 3. Architecture at a Glance
| Layer | Functional building blocks |
| --- | --- |
| UI/View models | `TaskResultDoBuilder` commands, `ReaderTaskResult<AppEnv, TResult>` workflows, `State<TState, TValue>` for filters, `Writer<TLog>` for toast feeds |
| Application services | `Option`, `Result`, `Validation`, `Lens`, `TaskResult`, `ReaderTaskResult`, `Continuation` (undo) |
| Infrastructure | SQLite repositories (`StateTaskResult` + `DbConnectionStateTaskResults`), HTTP enrichment (`HttpClientTaskResults`), file storage (`TaskIO`, `Try`, `IO`) |

Every workflow returns `TaskResult<TResult>` (or `ReaderTaskResult<AppEnvironment, TResult>` when DI is required) so UI commands can `await` deterministic success/failure values.

## 4. Scenario Walkthroughs

### Create & Validate a Customer
1. Click **New Customer**.
2. Fill in profile fields; validation badges update live.
3. Hit **Save**.

Under the hood:
- UI state is kept in a `State<EditorState, TValue>` pipeline to track dirty totals and undo history.
- `Validator<ContactDraft>` accumulates field errors; `Lens` optics focus into nested address/email fields.
- Persistence uses `StateTaskResult<DbTransactionState, Contact>` to wrap `INSERT` statements in a transaction, emitting `Writer<string>` logs for the audit trail.

### Attach Files & Undo Changes
1. Open an existing customer.
2. Drag/drop (or choose) files; they appear immediately.
3. Use **Undo** to roll back the last total/attachment change.

Modules involved:
- `TaskIO` + `Try` guard file copy/delete and expose `TaskResult<Unit>` responses to the UI.
- Undo stack is powered by `Continuation` + `State` snapshots, so each change can be replayed or rolled back deterministically.
- `Option<FileAttachment>` flows through the UI to represent “selected attachment”.

### Background Enrichment
1. Press **Sync Profiles**.
2. Observe toast notifications streaming in while HTTP calls run.

Modules involved:
- `HttpClientTaskResults` wrap `HttpClient` requests; `TaskResultDoBuilder` orchestrates the effect.
- Progress events are pushed through `ChannelWriterTaskResults`, then surfaced to the UI as a `Writer<string>` log feed.
- Imported data is merged with existing records via optics (`Lens.Compose`) and `Result` pipelines.

## 5. Functional Coverage Matrix
| Module | Where to see it |
| --- | --- |
| `Option` | Repository lookups (`FindContact`), attachment selection state, optional enrichment payloads |
| `Result` / `TaskResult` | Every command handler, export/import operations, HTTP effects |
| `Validation` | Customer editor, CSV importer, wizard steps |
| `Reader` / `ReaderTaskResult` | Workflows that bind `AppEnvironment` (database, clock, HTTP, telemetry) |
| `State` / `StateTaskResult` | UI filter state, transactional DB operations |
| `Writer` | Audit logs (`logs/telemetry.jsonl`), toast notifications |
| `Lens` | Nested updates (address, preferred channel), validation error prefixes |
| `TaskIO` / `IO` | Attachment manipulation, export pipelines |
| `Try` | File import/export safety net |
| `Effects` | `DbConnectionStateTaskResults`, `ChannelWriterTaskResults`, `HttpClientTaskResults` |
| `Continuation` | Undo stack |

## 6. Testing & Extensibility
- Workflows are exposed as `ReaderTaskResult<AppEnvironment, TResult>`, so you can inject fakes by creating a test `AppEnvironment` record and calling `.Run(fakeEnv)`.
- Add new commands by composing the same primitives: start with validation, layer in repository calls (`StateTaskResult`), and return `TaskResult<Unit>` to the UI.
- Snippets for upcoming tutorials will live under `docs/snippets/Samples/`; contributions should follow the snippet build process (`dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj`).

## 7. Troubleshooting
| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Runtime complains about preview features | Ensure .NET 10 SDK is installed and `dotnet --info` matches `global.json`. |
| Avalonia window fails to start on Linux | Install `libgtk-3-0` and `libicu`. |
| “Database locked” errors | Delete the `%LocalAppData%/FunctionalCRM` folder or run with `--seed` to rebuild. |
| Attachments missing after save | Check file system permissions; the sample writes under the same folder as the SQLite DB. |

## 8. Next Steps
- Pair this walkthrough with the how-to guides (`docs/how-to/*.md`) to implement new workflows.
- Use the benchmarking appendix (`docs/reference/benchmarks.md`) to compare FunctionalExtensions-based pipelines to imperative equivalents.
- Keep an eye on `docs/reference/api/` generated by DocFX once CI publishes the API site.
