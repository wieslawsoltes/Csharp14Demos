using System;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Monad helpers for <see cref="Option{T}"/> enabling Haskell-style query expressions.
/// </summary>
public static class OptionMonad
{
    extension<T>(Option<T>)
    {
        /// <summary>
        /// Wraps <paramref name="value"/> in <see cref="Option{T}.Some"/>.
        /// </summary>
        public static Option<T> Return(T value) => Option<T>.Some(value);
    }

    extension<T>(Option<T> option)
    {
        /// <summary>
        /// Chains computations that return options, mirroring Haskell's <c>&gt;&gt;=</c>.
        /// </summary>
        public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder)
        {
            ArgumentNullException.ThrowIfNull(binder);

            return option.HasValue ? binder(option.Value!) : Option<TResult>.None;
        }

        /// <summary>
        /// Enables LINQ query expressions on <see cref="Option{T}"/>.
        /// </summary>
        public Option<TResult> SelectMany<TResult>(Func<T, Option<TResult>> binder)
            => option.Bind(binder);

        /// <summary>
        /// Enables LINQ query expressions with projections.
        /// </summary>
        public Option<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Option<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            ArgumentNullException.ThrowIfNull(binder);
            ArgumentNullException.ThrowIfNull(projector);

            if (!option.HasValue)
            {
                return Option<TResult>.None;
            }

            var intermediate = binder(option.Value!);
            if (!intermediate.HasValue)
            {
                return Option<TResult>.None;
            }

            return Option<TResult>.Some(projector(option.Value!, intermediate.Value!));
        }
    }

    extension<T>(Option<Option<T>> nested)
    {
        /// <summary>
        /// Flattens a nested option one level deep.
        /// </summary>
        public Option<T> Join()
            => nested.Bind(static inner => inner);
    }
}
