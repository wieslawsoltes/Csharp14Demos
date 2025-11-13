using System;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // TaskIO (async) helpers.
    extension<T>(TaskIO<T> taskIO)
    {
        public Task<T> RunAsync()
            => taskIO.Invoke();

        public TaskIO<TResult> Map<TResult>(Func<T, TResult> selector)
            => TaskIO.From(async () => selector(await taskIO.Invoke().ConfigureAwait(false)));

        public TaskIO<TResult> Bind<TResult>(Func<T, TaskIO<TResult>> binder)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                return await binder(value).Invoke().ConfigureAwait(false);
            });

        public TaskIO<T> Tap(Func<T, Task> inspector)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                await inspector(value).ConfigureAwait(false);
                return value;
            });

        public TaskIO<TResult> Apply<TResult>(TaskIO<Func<T, TResult>> applicative)
            => TaskIO.From(async () =>
            {
                var func = await applicative.Invoke().ConfigureAwait(false);
                var value = await taskIO.Invoke().ConfigureAwait(false);
                return func(value);
            });

        public TaskIO<TResult> Select<TResult>(Func<T, TResult> selector)
            => taskIO.Map(selector);

        public TaskIO<TResult> SelectMany<TResult>(Func<T, TaskIO<TResult>> binder)
            => taskIO.Bind(binder);

        public TaskIO<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, TaskIO<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                var intermediate = await binder(value).Invoke().ConfigureAwait(false);
                return projector(value, intermediate);
            });

        public TaskIO<TResult> Then<TResult>(TaskIO<TResult> next)
            => TaskIO.From(async () =>
            {
                await taskIO.Invoke().ConfigureAwait(false);
                return await next.Invoke().ConfigureAwait(false);
            });

        public TaskIO<T> Delay(TimeSpan delay)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                await Task.Delay(delay).ConfigureAwait(false);
                return value;
            });

        public TaskIO<T> WithCancellation(CancellationToken token)
            => new(taskIO.Invoke().WaitAsync(token));

        public TaskIO<TResult> Using<TResource, TResult>(
            Func<T, TaskIO<TResource>> resourceFactory,
            Func<T, TResource, TaskIO<TResult>> body)
            where TResource : IAsyncDisposable
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                await using var resource = await resourceFactory(value).Invoke().ConfigureAwait(false);
                return await body(value, resource).Invoke().ConfigureAwait(false);
            });

        public Option<T> ToOption()
        {
            try
            {
                var value = taskIO.Invoke().GetAwaiter().GetResult();
                return value is null ? Option<T>.None : Option<T>.Some(value);
            }
            catch
            {
                return Option<T>.None;
            }
        }

        public async Task<Option<T>> ToOptionAsync()
        {
            try
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                return value is null ? Option<T>.None : Option<T>.Some(value);
            }
            catch
            {
                return Option<T>.None;
            }
        }

        public async Task<Result<T>> ToResultAsync(Func<Exception, string>? errorFactory = null)
        {
            try
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                return Result<T>.Ok(value);
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(errorFactory?.Invoke(ex) ?? ex.Message);
            }
        }

        public Try<T> ToTry()
        {
            try
            {
                var value = taskIO.Invoke().GetAwaiter().GetResult();
                return Try<T>.Success(value);
            }
            catch (Exception ex)
            {
                return Try<T>.Failure(ex);
            }
        }

        public async Task<Try<T>> ToTryAsync()
        {
            try
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                return Try<T>.Success(value);
            }
            catch (Exception ex)
            {
                return Try<T>.Failure(ex);
            }
        }

        public TaskIO<T> ToTaskIO()
            => taskIO;
    }
}
