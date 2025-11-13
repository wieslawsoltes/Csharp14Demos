using System;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Functor support for <see cref="Option{T}"/> that mirrors Haskell's <c>fmap</c> semantics.
/// </summary>
public static partial class Functor
{
    extension<T>(Option<T> option)
    {
        /// <summary>
        /// Maps the contents of the option using <paramref name="selector"/> when a value is present.
        /// </summary>
        public Option<TResult> FMap<TResult>(Func<T, TResult> selector)
            => option.HasValue ? Option<TResult>.Some(selector(option.Value!)) : Option<TResult>.None;

        /// <summary>
        /// Maps the option to a constant value when populated, mirroring Haskell's <c>(&lt;$)</c>.
        /// </summary>
        public Option<TResult> As<TResult>(TResult value)
            => option.HasValue ? Option<TResult>.Some(value) : Option<TResult>.None;

        /// <summary>
        /// Applies <paramref name="effect"/> for its side-effects when the option carries a value.
        /// </summary>
        public Option<T> Tap(Action<T> effect)
        {
            if (option.HasValue)
            {
                effect(option.Value!);
            }

            return option;
        }

        /// <summary>
        /// LINQ-friendly alias for <see cref="FMap"/>.
        /// </summary>
        public Option<TResult> Select<TResult>(Func<T, TResult> selector)
            => option.FMap(selector);
    }
}
