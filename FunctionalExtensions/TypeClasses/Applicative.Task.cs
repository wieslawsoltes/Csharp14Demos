using System;
using System.Threading.Tasks;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Applicative helpers for <see cref="Task{TResult}"/> providing expressive async composition.
/// </summary>
public static class TaskApplicative
{
    extension<T>(Task<T>)
    {
        /// <summary>
        /// Wraps <paramref name="value"/> in a completed <see cref="Task{TResult}"/>.
        /// </summary>
        public static Task<T> Pure(T value) => Task.FromResult(value);
    }

    extension<T>(Task<T> task)
    {
        /// <summary>
        /// Applies the asynchronous function carried by <paramref name="applicative"/> to the awaited value.
        /// </summary>
        public async Task<TResult> Ap<TResult>(Task<Func<T, TResult>> applicative)
        {
            ArgumentNullException.ThrowIfNull(applicative);

            var func = await applicative.ConfigureAwait(false);
            var value = await task.ConfigureAwait(false);
            return func(value);
        }

        /// <summary>
        /// Lifts a binary function into the <see cref="Task"/> applicative context.
        /// </summary>
        public async Task<TResult> LiftA2<TOther, TResult>(Task<TOther> other, Func<T, TOther, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(other);
            ArgumentNullException.ThrowIfNull(selector);

            var first = await task.ConfigureAwait(false);
            var second = await other.ConfigureAwait(false);
            return selector(first, second);
        }

        /// <summary>
        /// Maps the awaited value using <paramref name="selector"/>.
        /// </summary>
        public Task<TResult> Map<TResult>(Func<T, TResult> selector)
            => task.FMap(selector);

        /// <summary>
        /// Maps the awaited value using the asynchronous <paramref name="selector"/>.
        /// </summary>
        public Task<TResult> MapAsync<TResult>(Func<T, Task<TResult>> selector)
            => task.FMapAsync(selector);
    }

    extension<TArg, TResult>(Task<Func<TArg, TResult>> applicative)
    {
        /// <summary>
        /// Applies the function task to the value task using applicative sequencing semantics.
        /// </summary>
        public Task<TResult> Apply(Task<TArg> value)
            => value.Ap(applicative);
    }
}
