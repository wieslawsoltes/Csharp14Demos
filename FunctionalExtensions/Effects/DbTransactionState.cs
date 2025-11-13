using System.Data.Common;

namespace FunctionalExtensions.Effects;

/// <summary>
/// Represents the ambient transaction flowing through <see cref="StateTaskResult{TState, TValue}"/> pipelines.
/// </summary>
public readonly record struct DbTransactionState(DbTransaction? Transaction, bool OwnsTransaction)
{
    public bool HasTransaction => Transaction is not null;

    public static DbTransactionState Empty => new(null, false);
}
