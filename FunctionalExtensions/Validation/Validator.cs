using System;
using System.Collections.Generic;
using FunctionalExtensions.Optics;
using FunctionalExtensions;

namespace FunctionalExtensions.ValidationDsl;

/// <summary>
/// Delegate that evaluates a subject and produces a validation result.
/// </summary>
public delegate Validation<Unit> ValidationRule<in T>(T subject);

/// <summary>
/// Immutable collection of validation rules for <typeparamref name="T"/>.
/// </summary>
public sealed class Validator<T>
{
    private readonly ValidationRule<T>[] _rules;

    private Validator(ValidationRule<T>[] rules)
    {
        _rules = rules;
    }

    public static Validator<T> Empty { get; } = new(Array.Empty<ValidationRule<T>>());

    public IReadOnlyList<ValidationRule<T>> Rules => Array.AsReadOnly(_rules);

    public Validator<T> AddRule(ValidationRule<T> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var next = new ValidationRule<T>[_rules.Length + 1];
        if (_rules.Length > 0)
        {
            Array.Copy(_rules, next, _rules.Length);
        }

        next[^1] = rule;
        return new Validator<T>(next);
    }

    public Validator<T> Append(Validator<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other._rules.Length == 0)
        {
            return this;
        }

        if (_rules.Length == 0)
        {
            return other;
        }

        var merged = new ValidationRule<T>[_rules.Length + other._rules.Length];
        Array.Copy(_rules, merged, _rules.Length);
        Array.Copy(other._rules, 0, merged, _rules.Length, other._rules.Length);
        return new Validator<T>(merged);
    }

    public Validation<T> Validate(T subject)
    {
        if (_rules.Length == 0)
        {
            return Validation<T>.Success(subject);
        }

        var errors = ListPool<string>.Rent();

        try
        {
            foreach (var rule in _rules)
            {
                var result = rule(subject);
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }

            if (errors.Count == 0)
            {
                return Validation<T>.Success(subject);
            }

            return Validation<T>.Failure(errors.ToArray());
        }
        finally
        {
            ListPool<string>.Return(errors);
        }
    }
}

/// <summary>
/// Extension members that build fluent validation pipelines.
/// </summary>
public static class ValidatorExtensions
{
    extension<TSubject>(Validator<TSubject> validator)
    {
        /// <summary>
        /// Adds an ad-hoc validation rule that inspects the subject.
        /// </summary>
        public Validator<TSubject> Ensure(Func<TSubject, bool> predicate, string error)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(error);

            return validator.AddRule(subject =>
                predicate(subject)
                    ? Validation<Unit>.Success(Unit.Value)
                    : Validation<Unit>.Failure(error));
        }

        /// <summary>
        /// Adds a validation rule focused through a lens with a simple predicate.
        /// </summary>
        public Validator<TSubject> Ensure<TValue>(Lens<TSubject, TValue> lens, Func<TValue, bool> predicate, string error)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(error);

            return validator.AddRule(subject =>
            {
                var value = lens.Get(subject);
                return predicate(value)
                    ? Validation<Unit>.Success(Unit.Value)
                    : Validation<Unit>.Failure($"{lens.Describe()}: {error}");
            });
        }

        /// <summary>
        /// Adds a validation rule focused through a lens that uses a nested validator.
        /// </summary>
        public Validator<TSubject> Ensure<TValue>(Lens<TSubject, TValue> lens, Validator<TValue> nested)
        {
            ArgumentNullException.ThrowIfNull(nested);

            return validator.AddRule(subject =>
            {
                var value = lens.Get(subject);
                var result = nested.Validate(value);
                if (result.IsValid)
                {
                    return Validation<Unit>.Success(Unit.Value);
                }

                return Validation<Unit>.Failure(PrefixErrors(lens, result.Errors));
            });
        }

        /// <summary>
        /// Validates the subject against the configured rules.
        /// </summary>
        public Validation<TSubject> Apply(TSubject subject)
            => validator.Validate(subject);

        /// <summary>
        /// Concatenates <paramref name="other"/> validators.
        /// </summary>
        public Validator<TSubject> Append(Validator<TSubject> other)
            => validator.Append(other);
    }

    private static string[] PrefixErrors<TSubject, TValue>(Lens<TSubject, TValue> lens, IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return Array.Empty<string>();
        }

        var path = lens.Describe();
        var formatted = new string[errors.Count];

        for (var i = 0; i < errors.Count; i++)
        {
            formatted[i] = $"{path}: {errors[i]}";
        }

        return formatted;
    }
}

/// <summary>
/// Simple list pooling utility to reduce short-lived allocations inside validation loops.
/// </summary>
internal static class ListPool<T>
{
    [ThreadStatic]
    private static Stack<List<T>>? _cache;

    public static List<T> Rent()
    {
        var cache = _cache;
        if (cache is { Count: > 0 })
        {
            var list = cache.Pop();
            list.Clear();
            return list;
        }

        return new List<T>();
    }

    public static void Return(List<T> list)
    {
        list.Clear();

        (_cache ??= new Stack<List<T>>()).Push(list);
    }
}
