using System;
using System.Collections.Generic;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Minimal contract that mirrors Haskell's Monoid while embracing C# static abstract members.
/// </summary>
/// <typeparam name="TSelf">The concrete type that participates in the monoid.</typeparam>
public interface IMonoid<TSelf>
    where TSelf : IMonoid<TSelf>
{
    /// <summary>
    /// Identity element of the monoid.
    /// </summary>
    static abstract TSelf Empty { get; }

    /// <summary>
    /// Associative composition operation.
    /// </summary>
    static abstract TSelf Combine(TSelf left, TSelf right);
}

/// <summary>
/// Extension-powered helpers that illuminate monoid instances through familiar method names.
/// </summary>
public static class MonoidModule
{
    extension<T>(T value)
        where T : IMonoid<T>
    {
        /// <summary>
        /// Combines the current value with <paramref name="other"/> using the monoid instance.
        /// </summary>
        public T Append(T other)
            => T.Combine(value, other);

        /// <summary>
        /// Monoid identity.
        /// </summary>
        public static T Empty => T.Empty;
    }

    extension<T>(IEnumerable<T> source)
        where T : IMonoid<T>
    {
        /// <summary>
        /// Folds the sequence using the monoid's associative operation.
        /// </summary>
        public T ConcatAll()
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var result = T.Empty;
            foreach (var item in source)
            {
                result = T.Combine(result, item);
            }

            return result;
        }
    }

    /// <summary>
    /// Utility that creates monoid instances from delegates.
    /// </summary>
    public static Monoid<T> Create<T>(T empty, Func<T, T, T> combiner)
        => new(empty, combiner);
}

/// <summary>
/// Delegate-backed monoid instance for scenarios where a concrete type cannot implement <see cref="IMonoid{TSelf}"/>.
/// </summary>
public readonly record struct Monoid<T>(T Empty, Func<T, T, T> Combine);
