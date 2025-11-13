using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using FunctionalExtensions;

namespace FunctionalExtensions.Effects;

/// <summary>
/// Database helpers that adapt <see cref="DbConnection"/> operations into stateful <see cref="TaskResult{T}"/> workflows.
/// </summary>
public static class DbConnectionStateTaskResults
{
    extension(DbConnection connection)
    {
        /// <summary>
        /// Executes <paramref name="operation"/> inside a transaction-aware <see cref="StateTaskResult{TState, TValue}"/>.
        /// </summary>
        public StateTaskResult<DbTransactionState, TValue> ToStateTaskResult<TValue>(
            Func<DbConnection, DbTransaction, CancellationToken, Task<TValue>> operation,
            CancellationToken cancellationToken = default,
            bool beginIfMissing = true,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            ArgumentNullException.ThrowIfNull(operation);

            return new StateTaskResult<DbTransactionState, TValue>(state =>
                new TaskResult<(TValue Value, DbTransactionState State)>(ExecuteAsync(state)));

            async Task<Result<(TValue Value, DbTransactionState State)>> ExecuteAsync(DbTransactionState state)
            {
                DbTransaction? transaction = state.Transaction;
                var ownsTransaction = state.OwnsTransaction;

                try
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (transaction is null)
                    {
                        if (!beginIfMissing)
                        {
                            return Result<(TValue, DbTransactionState)>.Fail("Transaction state was empty and beginIfMissing=false.");
                        }

                        transaction = await BeginTransactionAsync(connection, isolationLevel, cancellationToken).ConfigureAwait(false);
                        ownsTransaction = true;
                    }

                    var value = await operation(connection, transaction, cancellationToken).ConfigureAwait(false);
                    return Result<(TValue, DbTransactionState)>.Ok((value, new DbTransactionState(transaction, ownsTransaction)));
                }
                catch (Exception ex)
                {
                    if (transaction is not null && ownsTransaction)
                    {
                        await SafeRollbackAsync(transaction, cancellationToken).ConfigureAwait(false);
                    }

                    return Result<(TValue, DbTransactionState)>.Fail(ex.Message);
                }
            }
        }

        /// <summary>
        /// Commits the ambient transaction when owned by the pipeline.
        /// </summary>
        public StateTaskResult<DbTransactionState, Unit> CommitTransaction(bool dispose = true, CancellationToken cancellationToken = default)
        {
            return new StateTaskResult<DbTransactionState, Unit>(state =>
                new TaskResult<(Unit Value, DbTransactionState State)>(ExecuteAsync(state)));

            async Task<Result<(Unit, DbTransactionState)>> ExecuteAsync(DbTransactionState state)
            {
                if (!state.HasTransaction)
                {
                    return Result<(Unit, DbTransactionState)>.Fail("No transaction available to commit.");
                }

                if (!state.OwnsTransaction)
                {
                    return Result<(Unit, DbTransactionState)>.Ok((Unit.Value, state));
                }

                try
                {
                    await state.Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                    if (dispose)
                    {
                        await state.Transaction.DisposeAsync().ConfigureAwait(false);
                    }

                    return Result<(Unit, DbTransactionState)>.Ok((Unit.Value, DbTransactionState.Empty));
                }
                catch (Exception ex)
                {
                    return Result<(Unit, DbTransactionState)>.Fail(ex.Message);
                }
            }
        }

        /// <summary>
        /// Rolls back the ambient transaction (if owned) and clears the state.
        /// </summary>
        public StateTaskResult<DbTransactionState, Unit> RollbackTransaction(bool dispose = true, CancellationToken cancellationToken = default)
        {
            return new StateTaskResult<DbTransactionState, Unit>(state =>
                new TaskResult<(Unit Value, DbTransactionState State)>(ExecuteAsync(state)));

            async Task<Result<(Unit, DbTransactionState)>> ExecuteAsync(DbTransactionState state)
            {
                if (!state.HasTransaction || !state.OwnsTransaction)
                {
                    return Result<(Unit, DbTransactionState)>.Ok((Unit.Value, state));
                }

                try
                {
                    await state.Transaction!.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    if (dispose)
                    {
                        await state.Transaction.DisposeAsync().ConfigureAwait(false);
                    }

                    return Result<(Unit, DbTransactionState)>.Ok((Unit.Value, DbTransactionState.Empty));
                }
                catch (Exception ex)
                {
                    return Result<(Unit, DbTransactionState)>.Fail(ex.Message);
                }
            }
        }
    }

    private static async Task<DbTransaction> BeginTransactionAsync(
        DbConnection connection,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        return await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
#else
        return await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#endif
    }

    private static async Task SafeRollbackAsync(DbTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Swallow rollback exceptions to surface original failure.
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
