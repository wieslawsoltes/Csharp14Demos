using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="TaskIO{T}"/>.
/// </summary>
public static class TaskIO
{
    public static TaskIO<T> Return<T>(T value)
        => new(Task.FromResult(value));

    public static TaskIO<T> From<T>(Func<Task<T>> producer)
    {
        ArgumentNullException.ThrowIfNull(producer);
        return new(producer());
    }

    public static TaskIO<Unit> From(Func<Task> producer)
    {
        ArgumentNullException.ThrowIfNull(producer);

        return new(ExecuteAsync(producer));

        static async Task<Unit> ExecuteAsync(Func<Task> action)
        {
            await action().ConfigureAwait(false);
            return Unit.Value;
        }
    }
}
