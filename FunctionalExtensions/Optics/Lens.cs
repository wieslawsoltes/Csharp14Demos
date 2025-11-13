using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FunctionalExtensions.Optics;

/// <summary>
/// Describes a focus into <typeparamref name="TSource"/> that can read and update a value of type <typeparamref name="TValue"/>.
/// </summary>
/// <param name="Getter">Function that extracts the focused value.</param>
/// <param name="Setter">Function that replaces the focused value.</param>
/// <param name="Path">Optional, human-readable path used for diagnostics.</param>
public readonly record struct Lens<TSource, TValue>(
    Func<TSource, TValue> Getter,
    Func<TSource, TValue, TSource> Setter,
    string? Path)
{
    public override string ToString()
        => Path is { Length: > 0 } ? $"Lens({Path})" : $"Lens({typeof(TSource).Name}->{typeof(TValue).Name})";
}

/// <summary>
/// Factory helpers for building <see cref="Lens{TSource, TValue}"/>.
/// </summary>
public static class Lens
{
    /// <summary>
    /// Creates a lens from explicit getter and setter functions.
    /// </summary>
    public static Lens<TSource, TValue> Create<TSource, TValue>(
        Func<TSource, TValue> getter,
        Func<TSource, TValue, TSource> setter,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        return new Lens<TSource, TValue>(getter, setter, path);
    }

    /// <summary>
    /// Builds a lens using a projection expression to capture property path information.
    /// </summary>
    public static Lens<TSource, TValue> From<TSource, TValue>(
        Expression<Func<TSource, TValue>> projection,
        Func<TSource, TValue, TSource> setter)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(setter);

        var getter = projection.Compile();
        var path = projection.TryGetMemberPath();
        return new Lens<TSource, TValue>(getter, setter, path);
    }

    /// <summary>
    /// Identity lens that focuses on the entire source value.
    /// </summary>
    public static Lens<T, T> Identity<T>()
        => Create<T, T>(
            static source => source,
            static (_, value) => value,
            "$");

    /// <summary>
    /// Combines two path segments into a dotted diagnostic path.
    /// </summary>
    internal static string? CombinePath(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return second;
        }

        if (string.IsNullOrWhiteSpace(second))
        {
            return first;
        }

        return $"{first}.{second}";
    }

    /// <summary>
    /// Attempts to resolve a member-access path for the supplied expression.
    /// </summary>
    private static string? TryGetMemberPath(this LambdaExpression expression)
    {
        static Expression Unwrap(Expression node)
            => node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary
                ? unary.Operand
                : node;

        var parts = new Stack<string>();
        var current = Unwrap(expression.Body);

        while (current is MemberExpression member)
        {
            parts.Push(member.Member.Name);
            current = Unwrap(member.Expression!);
        }

        return parts.Count > 0 ? string.Join(".", parts) : null;
    }
}
