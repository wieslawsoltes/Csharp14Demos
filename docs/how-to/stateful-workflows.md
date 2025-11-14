# How-To: Manage Stateful Workflows (Reader/Writer/State)

FunctionalExtensions ships lightweight Reader, Writer, and State abstractions so you can thread configuration, audit logs, and undo stacks through pure code. This guide shows how to combine them to build deterministic workflows before opting into async or IO effects.

All snippets compile in `docs/snippets/HowTo/StatefulWorkflows.cs`.

## 1. Inject configuration with `Reader`
The Reader monad passes an environment through your computation without parameter plumbing. Use it to build URLs, connection strings, or service clients that depend on runtime settings.

```csharp
public static Reader<ApiSettings, Uri> BuildProfileUri(Guid customerId)
    => Reader.From<ApiSettings, Uri>(settings =>
        new Uri($"{settings.BaseUrl.TrimEnd('/')}/customers/{customerId}") );
```
_Snippet: `StatefulWorkflows.cs#region reader_config`_

You can call `Reader<TEnv, TValue>.Local` to temporarily override sections of the environment (e.g., swap the base URL for a preview tenant) while keeping the same pipeline.

## 2. Accumulate audit logs with `Writer`
Writer pairs a value with a log list. Each transformation can add context without mutating shared state.

```csharp
public static Writer<bool, string> EvaluateOrderRisk(OrderDraft draft)
{
    var logs = new List<string>();

    if (draft.Total > 25_000)
    {
        logs.Add("High value order.");
    }

    if (draft.Currency != "USD")
    {
        logs.Add("FX exposure detected.");
    }

    var isRisky = draft.Total > draft.CreditLimit;
    logs.Add(isRisky ? "Credit limit exceeded." : "Within credit policy.");

    return new Writer<bool, string>(isRisky, logs);
}
```
_Snippet: `StatefulWorkflows.cs#region writer_audit`_

In larger pipelines, chain Writers with `SelectMany`/`Bind` so each step appends logs automatically.

## 3. Maintain an undo stack with `State`
State threads a mutable state record through pure functions. Here, we maintain invoice totals and a history of previous values to enable undo.

```csharp
public static State<EditorState, decimal> ApplyLineItem(decimal amount)
    => new(state =>
    {
        var history = SnapshotHistory(state);
        history.Add(state.Total);

        var updated = state with { Total = state.Total + amount, History = history.AsReadOnly() };
        return (updated.Total, updated);
    });

public static State<EditorState, decimal> UndoLastChange()
    => new(state =>
    {
        if (state.History.Count == 0)
        {
            return (state.Total, state);
        }

        var history = new List<decimal>(state.History);
        var previous = history[^1];
        history.RemoveAt(history.Count - 1);

        var updated = state with { Total = previous, History = history.AsReadOnly() };
        return (updated.Total, updated);
    });
```
_Snippet: `StatefulWorkflows.cs#region state_undo`_

Usage:
```csharp
var (totalAfterLines, state1) = StatefulWorkflows.ApplyLineItem(120m).Invoke(EditorState.Initial);
var (totalAfterUndo, finalState) = StatefulWorkflows.UndoLastChange().Invoke(state1);
```

## 4. Persist asynchronously with `StateTaskResult`
When it’s time to leave the pure world, switch to `StateTaskResult<TState, TValue>`. You can call an async saver, but still thread the logical state forward as part of the result.

```csharp
public static StateTaskResult<EditorState, decimal> PersistAsync(Func<EditorState, TaskResult<decimal>> saver)
    => new(state =>
        saver(state).Map(value => (value, state)));
```
_Snippet: `StatefulWorkflows.cs#region state_persist`_

Because `Map` is called on `TaskResult`, any failure (HTTP, database, validation) becomes a failed `TaskResult<(decimal, EditorState)>`, which short-circuits the state machine.

## 5. Mix Reader + Writer for review workflows
You can layer these abstractions: run validation through `Validator<T>`, attach logs via Writer, and still consume configuration from Reader.

```csharp
public static Reader<ApiSettings, Writer<OrderDraft, string>> BuildDraftReview(OrderDraft draft)
    => Reader.From<ApiSettings, Writer<OrderDraft, string>>(settings =>
    {
        var validation = DraftValidator.Apply(draft);
        if (!validation.IsValid)
        {
            return new Writer<OrderDraft, string>(draft, validation.Errors);
        }

        var logs = new List<string>
        {
            $"Priced with tax rate {settings.DefaultTaxRate:P0}."
        };

        return new Writer<OrderDraft, string>(draft, logs);
    });
```
_Snippet: `StatefulWorkflows.cs#region reader_writer_validator`_

This pattern makes it trivial to unit test complex workflows: supply a fake `ApiSettings`, run the reader, and inspect the returned logs without touching a database or UI.

## 6. Keep snippets honest
As you extend these patterns, update `docs/snippets/HowTo/StatefulWorkflows.cs` and run:

```bash
dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj
```

The build ensures your how-to documentation stays aligned with the actual FunctionalExtensions APIs.
