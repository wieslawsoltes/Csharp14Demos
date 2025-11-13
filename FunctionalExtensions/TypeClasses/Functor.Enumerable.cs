using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Functor helpers for <see cref="IEnumerable{T}"/> that emphasize lazy transformations.
/// </summary>
public static partial class Functor
{
    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// Deferred map over the sequence, mirroring Haskell's <c>fmap</c>.
        /// </summary>
        public IEnumerable<TResult> FMap<TResult>(Func<T, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return System.Linq.Enumerable.Select(source, selector);
        }

        /// <summary>
        /// Projects every element to <paramref name="value"/>, retaining laziness.
        /// </summary>
        public IEnumerable<TResult> As<TResult>(TResult value)
        {
            ArgumentNullException.ThrowIfNull(source);
            return System.Linq.Enumerable.Select(source, _ => value);
        }

        /// <summary>
        /// Executes <paramref name="effect"/> while streaming the original values.
        /// </summary>
        public IEnumerable<T> Tap(Action<T> effect)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(effect);

            return System.Linq.Enumerable.Select(source, item =>
            {
                effect(item);
                return item;
            });
        }

        /// <summary>
        /// LINQ-friendly alias for <see cref="FMap"/>.
        /// </summary>
        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
            => source.FMap(selector);
    }
}
