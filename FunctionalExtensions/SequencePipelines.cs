using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace FunctionalExtensions;

/// <summary>
/// Represents a reusable transformation that maps an <see cref="IEnumerable{T}"/> of <typeparamref name="TSource"/> into
/// an <see cref="IEnumerable{T}"/> of <typeparamref name="TResult"/>.
/// </summary>
public readonly record struct SequencePipe<TSource, TResult>(Func<IEnumerable<TSource>, IEnumerable<TResult>> Apply)
{
    public IEnumerable<TResult> Invoke(IEnumerable<TSource> source) => Apply(source);

    public SequencePipe<TSource, TNext> Then<TNext>(SequencePipe<TResult, TNext> next)
    {
        var apply = Apply;
        var nextApply = next.Apply;
        return new(source => nextApply(apply(source)));
    }

    public SequenceTerminal<TSource, TResultFinal> Then<TResultFinal>(SequenceTerminal<TResult, TResultFinal> terminal)
    {
        var apply = Apply;
        var terminalApply = terminal.Apply;
        return new(source => terminalApply(apply(source)));
    }

    public static IEnumerable<TResult> operator |(IEnumerable<TSource> source, SequencePipe<TSource, TResult> pipe)
        => pipe.Invoke(source);

    public static SequencePipe<TSource, TResult> operator +(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return firstApply(materializedSource).Concat(secondApply(materializedSource));
        });
    }

    public static SequencePipe<TSource, TResult> operator -(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return firstApply(materializedSource).Except(secondApply(materializedSource));
        });
    }

    public static SequencePipe<TSource, TResult> operator &(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return firstApply(materializedSource).Intersect(secondApply(materializedSource));
        });
    }

    public static SequencePipe<TSource, TResult> operator ^(SequencePipe<TSource, TResult> first, SequencePipe<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            var left = firstApply(materializedSource).ToList();
            var right = secondApply(materializedSource).ToList();
            return left.Except(right).Concat(right.Except(left));
        });
    }

    public static SequencePipe<TSource, TResult> operator ~(SequencePipe<TSource, TResult> pipe)
    {
        var apply = pipe.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return apply(materializedSource).Reverse();
        });
    }

    public static SequencePipe<TSource, TResult> operator *(SequencePipe<TSource, TResult> pipe, int repetitions)
    {
        if (repetitions < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repetitions));
        }

        var apply = pipe.Apply;
        return new(source =>
        {
            var materializedSource = source is IReadOnlyList<TSource> readOnly ? readOnly : source.ToList();
            return Enumerable.Range(0, repetitions).SelectMany(_ => apply(materializedSource));
        });
    }
}

/// <summary>
/// Represents a terminal operation that folds an <see cref="IEnumerable{T}"/> of <typeparamref name="TSource"/> into
/// a single <typeparamref name="TResult"/>.
/// </summary>
public readonly record struct SequenceTerminal<TSource, TResult>(Func<IEnumerable<TSource>, TResult> Apply)
{
    public TResult Invoke(IEnumerable<TSource> source) => Apply(source);

    public static TResult operator |(IEnumerable<TSource> source, SequenceTerminal<TSource, TResult> terminal)
        => terminal.Invoke(source);

    public SequenceTerminal<TSource, TResultNext> Select<TResultNext>(Func<TResult, TResultNext> projector)
    {
        var apply = Apply;
        return new(source => projector(apply(source)));
    }

    public static SequenceTerminal<TSource, (TResult First, TResult Second)> operator &(
        SequenceTerminal<TSource, TResult> first,
        SequenceTerminal<TSource, TResult> second)
    {
        var firstApply = first.Apply;
        var secondApply = second.Apply;
        return new(source =>
        {
            var materialized = source.ToList();
            return (firstApply(materialized), secondApply(materialized));
        });
    }
}

/// <summary>
/// Functional-style building blocks that surface common LINQ operations as reusable operator-enabled components.
/// </summary>
public static class SequenceOperators
{
    public static SequencePipe<TSource, TSource> Filter<TSource>(Func<TSource, bool> predicate)
        => new(source => source.Where(predicate));

    public static SequencePipe<TSource, TResult> Map<TSource, TResult>(Func<TSource, TResult> selector)
        => new(source => source.Select(selector));

    public static SequencePipe<TSource, TResult> Bind<TSource, TResult>(Func<TSource, IEnumerable<TResult>> selector)
        => new(source => source.SelectMany(selector));

    public static SequencePipe<TSource, TSource> Distinct<TSource>()
        => new(source => source.Distinct());

    public static SequencePipe<TSource, TSource> DistinctBy<TSource, TKey>(Func<TSource, TKey> keySelector)
        => new(source => source.DistinctBy(keySelector));

    public static SequencePipe<TSource, TSource> Append<TSource>(TSource value)
        => new(source => source.Append(value));

    public static SequencePipe<TSource, TSource> Prepend<TSource>(TSource value)
        => new(source => source.Prepend(value));

    public static SequencePipe<TSource, TSource> ConcatWith<TSource>(IEnumerable<TSource> other)
        => new(source => source.Concat(other));

    public static SequencePipe<TSource, TSource> DefaultIfEmpty<TSource>(TSource defaultValue)
        => new(source => source.DefaultIfEmpty(defaultValue));

    public static SequencePipe<TSource, TSource> Take<TSource>(int count)
        => new(source => source.Take(count));

    public static SequencePipe<TSource, TSource> Skip<TSource>(int count)
        => new(source => source.Skip(count));

    public static SequencePipe<TSource, TSource> SkipLast<TSource>(int count)
        => new(source => source.SkipLast(count));

    public static SequencePipe<TSource, TSource> TakeLast<TSource>(int count)
        => new(source => source.TakeLast(count));

    public static SequencePipe<TSource, TSource> Reverse<TSource>()
        => new(source => source.Reverse());

    public static SequencePipe<TSource, TSource> OrderBy<TSource, TKey>(Func<TSource, TKey> keySelector)
        => new(source => source.OrderBy(keySelector));

    public static SequencePipe<TSource, TSource> OrderByDescending<TSource, TKey>(Func<TSource, TKey> keySelector)
        => new(source => source.OrderByDescending(keySelector));

    public static SequencePipe<TSource, TSource> UnionWith<TSource>(IEnumerable<TSource> other, IEqualityComparer<TSource>? comparer = null)
        => new(source => comparer is null ? source.Union(other) : source.Union(other, comparer));

    public static SequencePipe<TSource, TSource> IntersectWith<TSource>(IEnumerable<TSource> other, IEqualityComparer<TSource>? comparer = null)
        => new(source => comparer is null ? source.Intersect(other) : source.Intersect(other, comparer));

    public static SequencePipe<TSource, TSource> ExceptWith<TSource>(IEnumerable<TSource> other, IEqualityComparer<TSource>? comparer = null)
        => new(source => comparer is null ? source.Except(other) : source.Except(other, comparer));

    public static SequencePipe<TSource, TSource> SymmetricExceptWith<TSource>(IEnumerable<TSource> other, IEqualityComparer<TSource>? comparer = null)
        => new(source =>
        {
            var left = comparer is null ? new HashSet<TSource>(source) : new HashSet<TSource>(source, comparer);
            var right = comparer is null ? new HashSet<TSource>(other) : new HashSet<TSource>(other, comparer);

            var difference = left.Except(right);
            var reverseDifference = right.Except(left);
            return difference.Concat(reverseDifference);
        });

    public static SequencePipe<TSource, IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(Func<TSource, TKey> keySelector)
        where TKey : notnull
        => new(source => source.GroupBy(keySelector));

    public static SequencePipe<TSource, TResult> GroupBy<TSource, TKey, TResult>(
        Func<TSource, TKey> keySelector,
        Func<TKey, IEnumerable<TSource>, TResult> projector)
        where TKey : notnull
        => new(source => source.GroupBy(keySelector, (key, values) => projector(key, values)));

    public static SequencePipe<TSource, TResult> Join<TSource, TInner, TKey, TResult>(
        IEnumerable<TInner> inner,
        Func<TSource, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TSource, TInner, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
        => new(source => comparer is null
            ? source.Join(inner, outerKeySelector, innerKeySelector, resultSelector)
            : source.Join(inner, outerKeySelector, innerKeySelector, resultSelector, comparer));

    public static SequencePipe<TSource, TResult> GroupJoin<TSource, TInner, TKey, TResult>(
        IEnumerable<TInner> inner,
        Func<TSource, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TSource, IEnumerable<TInner>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
        => new(source => comparer is null
            ? source.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector)
            : source.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector, comparer));

    public static SequencePipe<TSource, (TSource Item, IEnumerable<TInner> Matches)> LeftJoin<TSource, TInner, TKey>(
        IEnumerable<TInner> inner,
        Func<TSource, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
        => new(source => comparer is null
            ? source.GroupJoin(inner, outerKeySelector, innerKeySelector, (outer, matches) => (outer, matches))
            : source.GroupJoin(inner, outerKeySelector, innerKeySelector, (outer, matches) => (outer, matches), comparer));

    public static SequencePipe<TSource, (TSource Item, TInner? Match)> RightJoin<TSource, TInner, TKey>(
        IEnumerable<TInner> inner,
        Func<TSource, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
        => new(source =>
        {
            var lookup = comparer is null
                ? inner.ToLookup(innerKeySelector)
                : inner.ToLookup(innerKeySelector, comparer);

            return source.SelectMany(item =>
            {
                var key = outerKeySelector(item);
                var matches = lookup[key].ToList();
                if (matches.Count == 0)
                {
                    return new[] { (item, default(TInner?)) };
                }

                return matches.Select(match => (item, (TInner?)match));
            });
        });

    public static SequencePipe<TSource, TResult> Zip<TSource, TOther, TResult>(IEnumerable<TOther> other, Func<TSource, TOther, TResult> selector)
        => new(source => source.Zip(other, selector));

    public static SequencePipe<TSource, IReadOnlyList<TSource>> Chunk<TSource>(int size)
        => new(source => source.Chunk(size).Select(static chunk => (IReadOnlyList<TSource>)chunk.ToArray()));

    public static SequencePipe<TSource, (TSource Previous, TSource Current)> Pairwise<TSource>()
        => new(static source => PairwiseIterator(source));

    public static SequencePipe<TSource, IReadOnlyList<TSource>> Window<TSource>(int size, int step = 1, bool allowPartial = false)
        => new(source => WindowIterator(source, size, step, allowPartial));

    public static SequencePipe<TSource, TAccumulate> Scan<TSource, TAccumulate>(
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> aggregator)
        => new(source => ScanIterator(source, seed, aggregator));

    public static SequenceTerminal<TSource, List<TSource>> ToList<TSource>()
        => new(source => source.ToList());

    public static SequenceTerminal<TSource, TSource[]> ToArray<TSource>()
        => new(source => source.ToArray());

    public static SequenceTerminal<TSource, HashSet<TSource>> ToHashSet<TSource>()
        => new(source => source.ToHashSet());

    public static SequenceTerminal<TSource, Dictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>(
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector)
        where TKey : notnull
        => new(source => source.ToDictionary(keySelector, elementSelector));

    public static SequenceTerminal<TSource, int> Count<TSource>()
        => new(source => source.Count());

    public static SequenceTerminal<TSource, bool> Contains<TSource>(TSource value)
        => new(source => source.Contains(value));

    public static SequenceTerminal<TSource, bool> Any<TSource>()
        => new(source => source.Any());

    public static SequenceTerminal<TSource, bool> Any<TSource>(Func<TSource, bool> predicate)
        => new(source => source.Any(predicate));

    public static SequenceTerminal<TSource, bool> All<TSource>(Func<TSource, bool> predicate)
        => new(source => source.All(predicate));

    public static SequenceTerminal<TSource, TSource> First<TSource>()
        => new(source => source.First());

    public static SequenceTerminal<TSource, TSource> ElementAt<TSource>(int index)
        => new(source => source.ElementAt(index));

    public static SequenceTerminal<TSource, TSource> Last<TSource>()
        => new(source => source.Last());

    public static SequenceTerminal<TSource, TSource> Single<TSource>()
        => new(source => source.Single());

    public static SequenceTerminal<TSource, TSource?> Max<TSource>()
        => new(source => source.Max());

    public static SequenceTerminal<TSource, TSource?> Min<TSource>()
        => new(source => source.Min());

    public static SequenceTerminal<TSource, TSource?> MaxBy<TSource, TKey>(Func<TSource, TKey> keySelector)
        => new(source => source.MaxBy(keySelector));

    public static SequenceTerminal<TSource, TSource?> MinBy<TSource, TKey>(Func<TSource, TKey> keySelector)
        => new(source => source.MinBy(keySelector));

    public static SequenceTerminal<TSource, TSource> Sum<TSource>()
        where TSource : INumber<TSource>
        => new(source => source.Aggregate(TSource.Zero, static (acc, value) => acc + value));

    public static SequenceTerminal<TSource, double> Average<TSource>()
        where TSource : INumber<TSource>
        => new(source =>
        {
            var (sum, count) = source.Aggregate(
                (Sum: TSource.Zero, Count: 0),
                static (acc, value) => (acc.Sum + value, acc.Count + 1));

            if (count == 0)
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }

            return double.CreateChecked(sum) / count;
        });

    public static SequenceTerminal<TSource, TAccumulate> Aggregate<TSource, TAccumulate>(
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> aggregator)
        => new(source => source.Aggregate(seed, aggregator));

    public static SequenceTerminal<TSource, TResult> Aggregate<TSource, TAccumulate, TResult>(
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> aggregator,
        Func<TAccumulate, TResult> resultSelector)
        => new(source => source.Aggregate(seed, aggregator, resultSelector));

    public static SequenceTerminal<TSource, string> Join<TSource>(string separator, Func<TSource, string>? projector = null)
        => new(source =>
        {
            var items = projector is null ? source.Select(static item => item?.ToString() ?? string.Empty)
                                          : source.Select(projector);
            return string.Join(separator, items);
        });

    public static SequenceTerminal<TSource, bool> SequenceEqual<TSource>(IEnumerable<TSource> other, IEqualityComparer<TSource>? comparer = null)
        => new(source => comparer is null ? source.SequenceEqual(other) : source.SequenceEqual(other, comparer));

    private static IEnumerable<(TSource Previous, TSource Current)> PairwiseIterator<TSource>(IEnumerable<TSource> source)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            yield break;
        }

        var previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            yield return (previous, current);
            previous = current;
        }
    }

    private static IEnumerable<IReadOnlyList<TSource>> WindowIterator<TSource>(IEnumerable<TSource> source, int size, int step, bool allowPartial)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        var buffer = new Queue<TSource>(size);
        foreach (var item in source)
        {
            buffer.Enqueue(item);
            if (buffer.Count == size)
            {
                yield return buffer.ToArray();

                for (var i = 0; i < step && buffer.Count > 0; i++)
                {
                    buffer.Dequeue();
                }
            }
        }

        if (allowPartial && buffer.Count > 0)
        {
            yield return buffer.ToArray();
        }
    }

    private static IEnumerable<TAccumulate> ScanIterator<TSource, TAccumulate>(
        IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> aggregator)
    {
        var accumulator = seed;
        foreach (var item in source)
        {
            accumulator = aggregator(accumulator, item);
            yield return accumulator;
        }
    }
}
