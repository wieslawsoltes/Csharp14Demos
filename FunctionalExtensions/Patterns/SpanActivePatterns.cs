using System;
using System.Globalization;

using FunctionalExtensions;

namespace FunctionalExtensions.Patterns;

/// <summary>
/// Active patterns for working with spans in pattern matching scenarios.
/// </summary>
public static class SpanActivePatterns
{
    extension(ReadOnlySpan<char> span)
    {
        /// <summary>
        /// Attempts to parse the span into an <see cref="int"/> using invariant culture for pattern matching.
        /// </summary>
        public bool TryParseInt(out int value, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
            => int.TryParse(span, style, provider ?? CultureInfo.InvariantCulture, out value);

        /// <summary>
        /// Converts a successful parse to an <see cref="Option{T}"/> so it can be matched with the Option patterns.
        /// </summary>
        public Option<int> ToIntOption(NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
            => int.TryParse(span, style, provider ?? CultureInfo.InvariantCulture, out var value)
                ? Option<int>.Some(value)
                : Option<int>.None;
    }
}
