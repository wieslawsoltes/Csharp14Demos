using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    extension<T>(Task<T> task)
    {
        /// <summary>
        /// Maps the result of the task using a synchronous selector.
        /// </summary>
        public async Task<TResult> Map<TResult>(Func<T, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var value = await task.ConfigureAwait(false);
            return selector(value);
        }

        /// <summary>
        /// Chains asynchronous computations.
        /// </summary>
        public async Task<TResult> Bind<TResult>(Func<T, Task<TResult>> binder)
        {
            ArgumentNullException.ThrowIfNull(binder);
            var value = await task.ConfigureAwait(false);
            return await binder(value).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a side effect after the task completes.
        /// </summary>
        public async Task<T> Tap(Func<T, Task> effect)
        {
            ArgumentNullException.ThrowIfNull(effect);
            var value = await task.ConfigureAwait(false);
            await effect(value).ConfigureAwait(false);
            return value;
        }

        public Task<TResult> Select<TResult>(Func<T, TResult> selector)
            => task.Map(selector);

        public Task<TResult> SelectMany<TResult>(Func<T, Task<TResult>> binder)
            => task.Bind(binder);

        public async Task<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Task<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            ArgumentNullException.ThrowIfNull(binder);
            ArgumentNullException.ThrowIfNull(projector);

            var value = await task.ConfigureAwait(false);
            var intermediate = await binder(value).ConfigureAwait(false);
            return projector(value, intermediate);
        }
    }
}
