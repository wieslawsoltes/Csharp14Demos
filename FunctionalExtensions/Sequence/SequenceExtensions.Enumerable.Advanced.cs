using System;
using System.Collections.Generic;
using System.Linq;
using FunctionalExtensions.TypeClasses;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        /// <summary>
        /// Maps each element to a monoid and folds using the monoid's combine operation.
        /// </summary>
        public TMonoid FoldMap<TMonoid>(Func<TSource, TMonoid> selector)
            where TMonoid : IMonoid<TMonoid>
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            var result = TMonoid.Empty;
            foreach (var item in source)
            {
                var projected = selector(item);
                result = TMonoid.Combine(result, projected);
            }

            return result;
        }

        /// <summary>
        /// Traverses the sequence using an option-producing selector. Returns None if any projection fails.
        /// </summary>
        public Option<IReadOnlyList<TResult>> TraverseOption<TResult>(Func<TSource, Option<TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            var buffer = new List<TResult>();
            foreach (var item in source)
            {
                var projected = selector(item);
                if (!projected.HasValue)
                {
                    return Option<IReadOnlyList<TResult>>.None;
                }

                buffer.Add(projected.Value!);
            }

            return Option<IReadOnlyList<TResult>>.Some(buffer);
        }

        /// <summary>
        /// Splits the sequence into chunks while the predicate holds for consecutive items.
        /// </summary>
        public IEnumerable<IReadOnlyList<TSource>> ChunkWhile(Func<TSource, TSource, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                yield break;
            }

            var chunk = new List<TSource> { enumerator.Current };

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                var last = chunk[^1];

                if (predicate(last, current))
                {
                    chunk.Add(current);
                    continue;
                }

                yield return chunk.ToArray();
                chunk = new List<TSource> { current };
            }

            if (chunk.Count > 0)
            {
                yield return chunk.ToArray();
            }
        }

        /// <summary>
        /// Returns the tail of the sequence, or an empty sequence when the input is empty.
        /// </summary>
        public IEnumerable<TSource> Tail()
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Skip(1);
        }
    }

    extension<TSource>(ReadOnlySpan<TSource> span)
    {
        /// <summary>
        /// Returns the tail of the span by slicing off the first element.
        /// </summary>
        public ReadOnlySpan<TSource> Tail()
            => span.Length > 0 ? span[1..] : ReadOnlySpan<TSource>.Empty;
    }
}
