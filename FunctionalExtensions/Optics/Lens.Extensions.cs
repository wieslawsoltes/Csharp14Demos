using System;

namespace FunctionalExtensions.Optics;

/// <summary>
/// Extension-member powered helpers that make lenses ergonomic in C# 14.
/// </summary>
public static class LensExtensions
{
    extension<TSource, TValue>(Lens<TSource, TValue> lens)
    {
        /// <summary>
        /// Reads the focused value from <paramref name="source"/>.
        /// </summary>
        public TValue Get(TSource source)
        {
            EnsureInitialized(lens);
            return lens.Getter(source);
        }

        /// <summary>
        /// Replaces the focused value within <paramref name="source"/>.
        /// </summary>
        public TSource Set(TSource source, TValue value)
        {
            EnsureInitialized(lens);
            return lens.Setter(source, value);
        }

        /// <summary>
        /// Updates the focused value by applying <paramref name="projector"/>.
        /// </summary>
        public TSource Over(TSource source, Func<TValue, TValue> projector)
        {
            EnsureInitialized(lens);
            ArgumentNullException.ThrowIfNull(projector);

            var current = lens.Getter(source);
            var updated = projector(current);
            return lens.Setter(source, updated);
        }

        /// <summary>
        /// Composes the current lens with <paramref name="next"/>, yielding a new lens that zooms deeper into the structure.
        /// </summary>
        public Lens<TSource, TNext> Compose<TNext>(Lens<TValue, TNext> next)
        {
            EnsureInitialized(lens);
            if (next.Getter is null || next.Setter is null)
            {
                throw new ArgumentException("The provided lens is not initialized.", nameof(next));
            }

            return Lens.Create<TSource, TNext>(
                source =>
                {
                    var parent = lens.Getter(source);
                    return next.Getter(parent);
                },
                (source, value) =>
                {
                    var parent = lens.Getter(source);
                    var updatedParent = next.Setter(parent, value);
                    return lens.Setter(source, updatedParent);
                },
                Lens.CombinePath(lens.Path, next.Path));
        }

        /// <summary>
        /// Builds an error-friendly label for diagnostics.
        /// </summary>
        public string Describe()
            => !string.IsNullOrWhiteSpace(lens.Path)
                ? lens.Path!
                : $"{typeof(TSource).Name}.{typeof(TValue).Name}";

        private static void EnsureInitialized(Lens<TSource, TValue> value)
        {
            if (value.Getter is null || value.Setter is null)
            {
                throw new InvalidOperationException("Lens must be constructed via Lens.Create or Lens.From before use.");
            }
        }
    }
}
