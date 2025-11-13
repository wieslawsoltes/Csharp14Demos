using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    extension<T>(Lazy<T> lazy)
    {
        /// <summary>
        /// Maps the value produced by the lazy instance.
        /// </summary>
        public Lazy<TResult> Map<TResult>(Func<T, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return new Lazy<TResult>(() => selector(lazy.Value));
        }

        /// <summary>
        /// Chains computations that produce lazy values.
        /// </summary>
        public Lazy<TResult> Bind<TResult>(Func<T, Lazy<TResult>> binder)
        {
            ArgumentNullException.ThrowIfNull(binder);
            return new Lazy<TResult>(() => binder(lazy.Value).Value);
        }

        /// <summary>
        /// Eagerly evaluates side effects while returning the original lazy value.
        /// </summary>
        public Lazy<T> Tap(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            return new Lazy<T>(() =>
            {
                var value = lazy.Value;
                action(value);
                return value;
            });
        }

        public Lazy<TResult> Select<TResult>(Func<T, TResult> selector)
            => lazy.Map(selector);

        public Lazy<TResult> SelectMany<TResult>(Func<T, Lazy<TResult>> binder)
            => lazy.Bind(binder);

        public Lazy<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Lazy<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            ArgumentNullException.ThrowIfNull(binder);
            ArgumentNullException.ThrowIfNull(projector);

            return new Lazy<TResult>(() =>
            {
                var value = lazy.Value;
                var intermediate = binder(value).Value;
                return projector(value, intermediate);
            });
        }
    }
}
