using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

/// <summary>
/// Lightweight async wrapper that mirrors Haskell's Task/IO monad.
/// </summary>
public readonly record struct TaskIO<T>(Task<T> Task)
{
    public Task<T> Invoke() => Task;

    /// <summary>
    /// Converts the asynchronous effect into a <see cref="TaskResult{T}"/>, capturing thrown exceptions as failures.
    /// </summary>
    public TaskResult<T> ToTaskResult(Func<Exception, string>? errorFactory = null)
    {
        var task = Task;

        return TaskResults.From(async () =>
        {
            try
            {
                var value = await task.ConfigureAwait(false);
                return Result<T>.Ok(value);
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(errorFactory?.Invoke(ex) ?? ex.Message);
            }
        });
    }

    public static implicit operator TaskIO<T>(Task<T> task)
        => new(task);
}
