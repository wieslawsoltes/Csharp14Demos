using System;
using System.Threading.Tasks;

namespace FunctionalExtensions.TypeClasses;

/// <summary>
/// Monad helpers for <see cref="Task{TResult}"/> enabling fluent async workflows.
/// </summary>
public static class TaskMonad
{
    extension<T>(Task<T>)
    {
        /// <summary>
        /// Wraps <paramref name="value"/> in a completed <see cref="Task{TResult}"/>.
        /// </summary>
        public static Task<T> Return(T value) => Task.FromResult(value);
    }

    extension<T>(Task<T> task)
    {
        /// <summary>
        /// Chains asynchronous computations, mirroring Haskell's <c>&gt;&gt;=</c>.
        /// </summary>
        public async Task<TResult> Bind<TResult>(Func<T, Task<TResult>> binder)
        {
            ArgumentNullException.ThrowIfNull(binder);

            var value = await task.ConfigureAwait(false);
            return await binder(value).ConfigureAwait(false);
        }

        /// <summary>
        /// LINQ-friendly alias for <see cref="Bind"/>.
        /// </summary>
        public Task<TResult> SelectMany<TResult>(Func<T, Task<TResult>> binder)
            => task.Bind(binder);

        /// <summary>
        /// LINQ-friendly overload that performs projection after binding.
        /// </summary>
        public async Task<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Task<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            ArgumentNullException.ThrowIfNull(binder);
            ArgumentNullException.ThrowIfNull(projector);

            var value = await task.ConfigureAwait(false);
            var intermediate = await binder(value).ConfigureAwait(false);
            return projector(value, intermediate);
        }
    }

    extension<T>(Task<Task<T>> nested)
    {
        /// <summary>
        /// Flattens a nested task.
        /// </summary>
        public async Task<T> Join()
        {
            var inner = await nested.ConfigureAwait(false);
            return await inner.ConfigureAwait(false);
        }
    }
}
