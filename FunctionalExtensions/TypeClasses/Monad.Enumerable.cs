using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Monad helpers for <see cref="IEnumerable{T}"/> mimicking Haskell's list monad.
/// </summary>
public static class EnumerableMonad
{
    extension<T>(IEnumerable<T>)
    {
        /// <summary>
        /// Wraps <paramref name="value"/> in a singleton sequence.
        /// </summary>
        public static IEnumerable<T> Return(T value)
            => Enumerable.Repeat(value, 1);
    }

    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// Chains sequence-producing computations, mirroring Haskell's <c>&gt;&gt;=</c>.
        /// </summary>
        public IEnumerable<TResult> Bind<TResult>(Func<T, IEnumerable<TResult>> binder)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(binder);

            return System.Linq.Enumerable.SelectMany(source, binder);
        }

        /// <summary>
        /// LINQ-friendly alias for <see cref="Bind"/>.
        /// </summary>
        public IEnumerable<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> binder)
            => source.Bind(binder);

        /// <summary>
        /// LINQ-friendly overload that performs projection after binding.
        /// </summary>
        public IEnumerable<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, IEnumerable<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(binder);
            ArgumentNullException.ThrowIfNull(projector);

            return System.Linq.Enumerable.SelectMany(
                source,
                item => System.Linq.Enumerable.Select(binder(item), intermediate => projector(item, intermediate)));
        }
    }

    extension<T>(IEnumerable<IEnumerable<T>> nested)
    {
        /// <summary>
        /// Flattens a sequence of sequences by one level.
        /// </summary>
        public IEnumerable<T> Join()
        {
            ArgumentNullException.ThrowIfNull(nested);
            return System.Linq.Enumerable.SelectMany(nested, inner => inner);
        }
    }
}
