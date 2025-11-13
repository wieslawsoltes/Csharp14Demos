namespace FunctionalExtensions;

/// <summary>
/// Construction helpers for <see cref="TaskResult{T}"/>.
/// </summary>
public static partial class TaskResults
{
    public static TaskResult<T> Return<T>(T value)
        => new(Task.FromResult(Result<T>.Ok(value)));

    public static TaskResult<T> Fail<T>(string error)
        => new(Task.FromResult(Result<T>.Fail(error)));

    public static TaskResult<T> FromResult<T>(Result<T> result)
        => new(Task.FromResult(result));

    public static TaskResult<T> From<T>(Func<Task<Result<T>>> producer)
        => new(producer());

    public static TaskResult<T> From<T>(Func<Task<T>> producer)
        => new(ExecuteAsync(producer));

    public static TaskResult<T> FromTask<T>(Task<T> task)
        => new(ExecuteAsync(() => task));

    private static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> producer)
    {
        try
        {
            var value = await producer().ConfigureAwait(false);
            return Result<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex.Message);
        }
    }
}
