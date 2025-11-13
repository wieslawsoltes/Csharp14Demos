using System.Diagnostics.CodeAnalysis;

using FunctionalExtensions;

namespace FunctionalExtensions.Patterns;

/// <summary>
/// Option-focused pattern helpers that make switch expressions more expressive.
/// </summary>
public static class OptionPatterns
{
    extension<T>(T? value)
        where T : class
    {
        /// <summary>
        /// Converts a potentially-null reference into an <see cref="Option{T}"/> so it can participate in pattern matching.
        /// </summary>
        public Option<T> AsOption()
            => Option.FromNullable(value);
    }

    extension<T>(T? value)
        where T : struct
    {
        /// <summary>
        /// Converts a nullable value type into an <see cref="Option{T}"/> for pattern-friendly matching.
        /// </summary>
        public Option<T> AsOption()
            => Option.FromNullable(value);
    }

    extension<T>(Option<T> option)
    {
        /// <summary>
        /// Active pattern that succeeds when the option carries a value.
        /// </summary>
        public bool Some([NotNullWhen(true)] out T value)
        {
            if (option.HasValue)
            {
                value = option.Value!;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Active pattern that indicates the option is empty.
        /// </summary>
        public bool None => !option.HasValue;
    }
}
