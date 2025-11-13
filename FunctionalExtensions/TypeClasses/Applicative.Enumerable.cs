using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Applicative helpers for <see cref="IEnumerable{T}"/> that mirror nondeterministic function application.
/// </summary>
public static class EnumerableApplicative
{
    extension<T>(IEnumerable<T>)
    {
        /// <summary>
        /// Wraps <paramref name="value"/> in a singleton sequence.
        /// </summary>
        public static IEnumerable<T> Pure(T value)
            => Enumerable.Repeat(value, 1);
    }

    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// Applies each function from <paramref name="applicative"/> to every value in the source sequence.
        /// </summary>
        public IEnumerable<TResult> Ap<TResult>(IEnumerable<Func<T, TResult>> applicative)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(applicative);

            return System.Linq.Enumerable.SelectMany(applicative, func => System.Linq.Enumerable.Select(source, func));
        }

        /// <summary>
        /// Lifts a binary function into the sequence applicative context.
        /// </summary>
        public IEnumerable<TResult> LiftA2<TOther, TResult>(IEnumerable<TOther> other, Func<T, TOther, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(other);
            ArgumentNullException.ThrowIfNull(selector);

            return System.Linq.Enumerable.SelectMany(source, first => System.Linq.Enumerable.Select(other, second => selector(first, second)));
        }

        /// <summary>
        /// Convenience map that forwards to the Functor implementation.
        /// </summary>
        public IEnumerable<TResult> Map<TResult>(Func<T, TResult> selector)
            => source.FMap(selector);
    }

    extension<TArg, TResult>(IEnumerable<Func<TArg, TResult>> applicative)
    {
        /// <summary>
        /// Applies the sequence of functions to <paramref name="values"/> using applicative semantics.
        /// </summary>
        public IEnumerable<TResult> Apply(IEnumerable<TArg> values)
        {
            ArgumentNullException.ThrowIfNull(applicative);
            ArgumentNullException.ThrowIfNull(values);

            return System.Linq.Enumerable.SelectMany(applicative, func => System.Linq.Enumerable.Select(values, func));
        }
    }
}
