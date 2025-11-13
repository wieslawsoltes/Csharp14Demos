using System;
using System.Threading.Tasks;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    extension<T>(ValueTask<T> task)
    {
        public async ValueTask<TResult> Map<TResult>(Func<T, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var value = await task.ConfigureAwait(false);
            return selector(value);
        }

        public async ValueTask<TResult> Bind<TResult>(Func<T, ValueTask<TResult>> binder)
        {
            ArgumentNullException.ThrowIfNull(binder);
            var value = await task.ConfigureAwait(false);
            return await binder(value).ConfigureAwait(false);
        }

        public async ValueTask<T> Tap(Func<T, ValueTask> effect)
        {
            ArgumentNullException.ThrowIfNull(effect);
            var value = await task.ConfigureAwait(false);
            await effect(value).ConfigureAwait(false);
            return value;
        }

        public ValueTask<TResult> Select<TResult>(Func<T, TResult> selector)
            => task.Map(selector);

        public ValueTask<TResult> SelectMany<TResult>(Func<T, ValueTask<TResult>> binder)
            => task.Bind(binder);

        public async ValueTask<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, ValueTask<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            ArgumentNullException.ThrowIfNull(binder);
            ArgumentNullException.ThrowIfNull(projector);

            var value = await task.ConfigureAwait(false);
            var intermediate = await binder(value).ConfigureAwait(false);
            return projector(value, intermediate);
        }
    }
}
