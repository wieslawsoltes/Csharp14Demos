using System;
using System.Collections.Generic;
using FunctionalExtensions;
using FunctionalExtensions.ValidationDsl;

namespace FunctionalExtensions.Snippets.HowTo;

public static class StatefulWorkflows
{
    #region reader_config
    public static Reader<ApiSettings, Uri> BuildProfileUri(Guid customerId)
        => Reader.From<ApiSettings, Uri>(settings =>
            new Uri($"{settings.BaseUrl.TrimEnd('/')}/customers/{customerId}") );
    #endregion

    #region writer_audit
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
    #endregion

    #region state_undo
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
    #endregion

    #region state_persist
    public static StateTaskResult<EditorState, decimal> PersistAsync(Func<EditorState, TaskResult<decimal>> saver)
        => new(state =>
            saver(state).Map(value => (value, state)));
    #endregion

    #region reader_writer_validator
    public static Validator<OrderDraft> DraftValidator { get; } = Validator<OrderDraft>.Empty
        .Ensure(d => !string.IsNullOrWhiteSpace(d.CustomerId), "CustomerId is required.")
        .Ensure(d => d.Total > 0, "Total must be positive.")
        .Ensure(d => d.Currency.Length == 3, "Currency must be ISO-4217.");

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
    #endregion

    private static List<decimal> SnapshotHistory(EditorState state)
        => state.History.Count == 0
            ? new List<decimal>()
            : new List<decimal>(state.History);
}

public sealed record ApiSettings(string BaseUrl, decimal DefaultTaxRate);
public sealed record OrderDraft(string CustomerId, decimal Total, decimal CreditLimit, string Currency);

public sealed record EditorState(decimal Total, IReadOnlyList<decimal> History)
{
    public static EditorState Initial { get; } = new(0m, Array.Empty<decimal>());
}
