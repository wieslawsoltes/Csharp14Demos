using System;
using System.Numerics;

namespace FunctionalExtensions.Numerics;

/// <summary>
/// Immutable rational number that supports generic math and monoid semantics.
/// </summary>
public readonly record struct Rational<T>(T Numerator, T Denominator)
    : IEquatable<Rational<T>>, IComparable<Rational<T>>
    where T : INumber<T>
{
    public T Value => Numerator / Denominator;

    public int CompareTo(Rational<T> other)
        => Value.CompareTo(other.Value);

    public override string ToString()
        => $"{Numerator}/{Denominator}";

    public static Rational<T> Zero => new(T.Zero, T.One);
    public static Rational<T> One => new(T.One, T.One);

    public static Rational<T> operator +(Rational<T> left, Rational<T> right)
        => Normalize(left.Numerator * right.Denominator + right.Numerator * left.Denominator,
            left.Denominator * right.Denominator);

    public static Rational<T> operator -(Rational<T> left, Rational<T> right)
        => Normalize(left.Numerator * right.Denominator - right.Numerator * left.Denominator,
            left.Denominator * right.Denominator);

    public static Rational<T> operator *(Rational<T> left, Rational<T> right)
        => Normalize(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

    public static Rational<T> operator /(Rational<T> left, Rational<T> right)
        => Normalize(left.Numerator * right.Denominator, left.Denominator * right.Numerator);

    public static implicit operator Rational<T>(T value)
        => new(value, T.One);

    public static implicit operator T(Rational<T> value)
        => value.Value;

    private static Rational<T> Normalize(T numerator, T denominator)
    {
        if (denominator == T.Zero)
        {
            throw new DivideByZeroException("Denominator cannot be zero.");
        }

        var gcd = GreatestCommonDivisor(numerator, denominator);
        numerator /= gcd;
        denominator /= gcd;

        if (denominator < T.Zero)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        return new Rational<T>(numerator, denominator);
    }

    private static T GreatestCommonDivisor(T a, T b)
    {
        a = T.Abs(a);
        b = T.Abs(b);

        while (b != T.Zero)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }

        return a;
    }
}
