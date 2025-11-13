using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using FunctionalExtensions;

namespace FunctionalExtensions.Computation;

/// <summary>
/// Builder that enables Haskell-style do-notation for <see cref="TaskResult{T}"/>.
/// </summary>
public readonly struct TaskResultDoBuilder
{
    /// <summary>
    /// Runs the computation and returns a <see cref="TaskResult{T}"/>.
    /// </summary>
    public TaskResult<TResult> Run<TResult>(Func<TaskResultDoScope, ValueTask<TResult>> workflow)
        => new(ExecuteInternalAsync(workflow));

    /// <summary>
    /// Runs the computation and returns a <see cref="TaskResult{T}"/>.
    /// </summary>
    public TaskResult<TResult> Run<TResult>(Func<TaskResultDoScope, Task<TResult>> workflow)
        => new(ExecuteInternalAsync(scope => new ValueTask<TResult>(workflow(scope))));

    /// <summary>
    /// Runs the computation and returns the underlying <see cref="Result{T}"/>.
    /// </summary>
    public Task<Result<TResult>> ExecuteAsync<TResult>(Func<TaskResultDoScope, ValueTask<TResult>> workflow)
        => ExecuteInternalAsync(workflow);

    /// <summary>
    /// Runs the computation and returns the underlying <see cref="Result{T}"/>.
    /// </summary>
    public Task<Result<TResult>> ExecuteAsync<TResult>(Func<TaskResultDoScope, Task<TResult>> workflow)
        => ExecuteInternalAsync(scope => new ValueTask<TResult>(workflow(scope)));

    private static async Task<Result<TResult>> ExecuteInternalAsync<TResult>(Func<TaskResultDoScope, ValueTask<TResult>> workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        try
        {
            var scope = new TaskResultDoScope();
            var value = await workflow(scope).ConfigureAwait(false);
            return Result<TResult>.Ok(value);
        }
        catch (TaskResultShortCircuitException ex)
        {
            return Result<TResult>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<TResult>.Fail(ex.Message);
        }
    }
}

/// <summary>
/// Scope that exposes helper methods consumed by do-notation workflows.
/// </summary>
public sealed class TaskResultDoScope
{
    /// <summary>
    /// Awaits the supplied <see cref="TaskResult{T}"/> and either returns its value or short-circuits the computation.
    /// </summary>
    public async ValueTask<T> Bind<T>(TaskResult<T> taskResult)
    {
        var result = await taskResult.Invoke().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new TaskResultShortCircuitException(result.Error ?? "Unknown error");
        }

        return result.Value!;
    }

    /// <summary>
    /// Alias for <see cref="Bind{T}(TaskResult{T})"/> that enables terse <c>await do.Await(...)</c> syntax.
    /// </summary>
    public ValueTask<T> Await<T>(TaskResult<T> taskResult)
        => Bind(taskResult);

    /// <summary>
    /// Converts a <see cref="Result{T}"/> into the computation, short-circuiting when it represents an error.
    /// </summary>
    public ValueTask<T> FromResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            throw new TaskResultShortCircuitException(result.Error ?? "Unknown error");
        }

        return ValueTask.FromResult(result.Value!);
    }

    /// <summary>
    /// Awaits a regular <see cref="Task{TResult}"/> and treats thrown exceptions as computation failures.
    /// </summary>
    public async ValueTask<T> FromTask<T>(Task<T> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new TaskResultShortCircuitException(ex.Message);
        }
    }

    /// <summary>
    /// Ensures the predicate holds, otherwise short-circuits with <paramref name="error"/>.
    /// </summary>
    public void Ensure(bool condition, string error)
    {
        if (!condition)
        {
            throw new TaskResultShortCircuitException(error);
        }
    }

    /// <summary>
    /// Wraps <paramref name="value"/> as the computation result.
    /// </summary>
    public ValueTask<T> Return<T>(T value)
        => ValueTask.FromResult(value);

    /// <summary>
    /// Awaits a resource-producing <see cref="TaskResult{T}"/> and surfaces it for <c>await using</c>.
    /// </summary>
    public async ValueTask<T> Use<T>(TaskResult<T> resource)
        where T : IAsyncDisposable
        => await Bind(resource).ConfigureAwait(false);

    /// <summary>
    /// Awaits an asynchronous sequence wrapped in <see cref="TaskResult{T}"/> so the computation can iterate it via <c>await foreach</c>.
    /// </summary>
    public async IAsyncEnumerable<T> ForEach<T>(TaskResult<IAsyncEnumerable<T>> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sequence = await Bind(source).ConfigureAwait(false);
        await foreach (var item in sequence.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Iterates an asynchronous sequence directly when no <see cref="TaskResult{T}"/> wrapper is involved.
    /// </summary>
    public async IAsyncEnumerable<T> ForEach<T>(IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}

internal sealed class TaskResultShortCircuitException : Exception
{
    public TaskResultShortCircuitException(string message)
        : base(string.IsNullOrWhiteSpace(message) ? "Unknown error" : message)
    {
    }
}
