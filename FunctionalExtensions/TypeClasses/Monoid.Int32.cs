namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Monoid instance that sums <see cref="int"/> values.
/// </summary>
public readonly record struct SumInt(int Value) : IMonoid<SumInt>
{
    public static SumInt Empty => new(0);

    public static SumInt Combine(SumInt left, SumInt right)
        => new(left.Value + right.Value);

    public override string ToString() => Value.ToString();

    public static implicit operator int(SumInt sum) => sum.Value;
    public static implicit operator SumInt(int value) => new(value);
}

/// <summary>
/// Monoid instance that multiplies <see cref="int"/> values.
/// </summary>
public readonly record struct ProductInt(int Value) : IMonoid<ProductInt>
{
    public static ProductInt Empty => new(1);

    public static ProductInt Combine(ProductInt left, ProductInt right)
        => new(left.Value * right.Value);

    public override string ToString() => Value.ToString();

    public static implicit operator int(ProductInt product) => product.Value;
    public static implicit operator ProductInt(int value) => new(value);
}
