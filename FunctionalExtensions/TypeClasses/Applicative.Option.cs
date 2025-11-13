using System;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Applicative helpers for <see cref="Option{T}"/> that align with Haskell's <c>Applicative</c> type class.
/// </summary>
public static class OptionApplicative
{
    extension<T>(Option<T>)
    {
        /// <summary>
        /// Lifts <paramref name="value"/> into an <see cref="Option{T}"/>.
        /// </summary>
        public static Option<T> Pure(T value) => Option<T>.Some(value);
    }

    extension<T>(Option<T> option)
    {
        /// <summary>
        /// Applies the optional function in <paramref name="applicative"/> to the current option.
        /// </summary>
        public Option<TResult> Ap<TResult>(Option<Func<T, TResult>> applicative)
            => option.HasValue && applicative.HasValue
                ? Option<TResult>.Some(applicative.Value!(option.Value!))
                : Option<TResult>.None;

        /// <summary>
        /// Lifts a binary function into the <see cref="Option{T}"/> context.
        /// </summary>
        public Option<TResult> LiftA2<TOther, TResult>(Option<TOther> other, Func<T, TOther, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            if (option.HasValue && other.HasValue)
            {
                return Option<TResult>.Some(selector(option.Value!, other.Value!));
            }

            return Option<TResult>.None;
        }

        /// <summary>
        /// Transforms the optional value into an applicative result using <paramref name="selector"/>.
        /// </summary>
        public Option<TResult> Map<TResult>(Func<T, TResult> selector)
            => option.HasValue ? Option<TResult>.Some(selector(option.Value!)) : Option<TResult>.None;
    }

    extension<TArg, TResult>(Option<Func<TArg, TResult>> applicative)
    {
        /// <summary>
        /// Enables operator-based application, mirroring Haskell's <c>&lt;*&gt;</c>.
        /// </summary>
        public Option<TResult> Apply(Option<TArg> value)
            => value.Ap(applicative);
    }
}
