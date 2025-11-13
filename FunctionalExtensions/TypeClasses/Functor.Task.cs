using System;
using System.Threading.Tasks;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Functor helpers for <see cref="Task{TResult}"/> that keep async flows expressive.
/// </summary>
public static partial class Functor
{
    extension<T>(Task<T> task)
    {
        /// <summary>
        /// Applies the synchronous <paramref name="selector"/> to the awaited value.
        /// </summary>
        public async Task<TResult> FMap<TResult>(Func<T, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var value = await task.ConfigureAwait(false);
            return selector(value);
        }

        /// <summary>
        /// Applies the asynchronous <paramref name="selector"/> to the awaited value.
        /// </summary>
        public async Task<TResult> FMapAsync<TResult>(Func<T, Task<TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var value = await task.ConfigureAwait(false);
            return await selector(value).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes <paramref name="effect"/> for its side-effects while forwarding the original task result.
        /// </summary>
        public async Task<T> Tap(Action<T> effect)
        {
            ArgumentNullException.ThrowIfNull(effect);

            var value = await task.ConfigureAwait(false);
            effect(value);
            return value;
        }

        /// <summary>
        /// Executes <paramref name="effect"/> asynchronously for its side-effects while forwarding the original task result.
        /// </summary>
        public async Task<T> TapAsync(Func<T, Task> effect)
        {
            ArgumentNullException.ThrowIfNull(effect);

            var value = await task.ConfigureAwait(false);
            await effect(value).ConfigureAwait(false);
            return value;
        }

        /// <summary>
        /// LINQ-friendly alias for <see cref="FMap"/>.
        /// </summary>
        public Task<TResult> Select<TResult>(Func<T, TResult> selector)
            => task.FMap(selector);
    }
}
