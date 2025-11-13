using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalExtensions;

/// <summary>
/// Minimal Option (Maybe) type that supports functional-style composition via user-defined operators.
/// </summary>
public readonly record struct Option<T>(bool HasValue, T? Value)
{
    public static Option<T> Some(T value)
        => new(true, value);

    public static Option<T> None
        => new(false, default);
}

/// <summary>
/// Result monad representing either a successful value or an error message.
/// </summary>
public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Ok(T value)
        => new(true, value, null);

    public static Result<T> Fail(string error)
        => new(false, default, error);

    public override string ToString()
        => IsSuccess ? $"Ok({Value})" : $"Error({Error})";
}

/// <summary>
/// Try monad wraps computations that may throw, capturing the exception instead of throwing.
/// </summary>
public readonly record struct Try<T>(bool IsSuccess, T? Value, Exception? Exception)
{
    public static Try<T> Success(T value)
        => new(true, value, null);

    public static Try<T> Failure(Exception exception)
        => new(false, default, exception);

    public T GetOrThrow()
        => IsSuccess ? Value! : throw Exception ?? new InvalidOperationException("Try was not successful.");

    public override string ToString()
        => IsSuccess ? $"Success({Value})" : $"Failure({Exception?.Message ?? "<unknown>"})";
}

/// <summary>
/// Lightweight unit type that plays nicely with IO style workflows.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();

    public override string ToString() => "()";
}

/// <summary>
/// Lazy IO monad that defers computation until explicitly run.
/// </summary>
public readonly record struct IO<T>(Func<T> Effect)
{
    public T Invoke() => Effect();

    public static implicit operator IO<T>(Func<T> effect)
        => new(effect);
}

/// <summary>
/// Task-result fusion that marries async workflows with success/error signalling.
/// </summary>
public readonly record struct TaskResult<T>(Task<Result<T>> Task)
{
    public Task<Result<T>> Invoke() => Task;

    public static implicit operator TaskResult<T>(Task<Result<T>> task)
        => new(task);
}

/// <summary>
/// Reader transformer for <see cref="TaskResult{T}"/> enabling environment-aware async computations.
/// </summary>
public readonly record struct ReaderTaskResult<TEnv, TValue>(Func<TEnv, TaskResult<TValue>> Run)
{
    public TaskResult<TValue> Invoke(TEnv environment) => Run(environment);
}

/// <summary>
/// State transformer for <see cref="TaskResult{T}"/> enabling async state threading.
/// </summary>
public readonly record struct StateTaskResult<TState, TValue>(Func<TState, TaskResult<(TValue Value, TState State)>> Run)
{
    public TaskResult<(TValue Value, TState State)> Invoke(TState state)
        => Run(state);
}

/// <summary>
/// Writer transformer for <see cref="TaskResult{T}"/> enabling asynchronous log accumulation.
/// </summary>
public readonly record struct WriterTaskResult<TValue, TLog>(TaskResult<(TValue Value, IReadOnlyList<TLog> Logs)> Run)
{
    public TaskResult<(TValue Value, IReadOnlyList<TLog> Logs)> Invoke() => Run;
}

/// <summary>
/// State monad threads mutable state through pure computations.
/// </summary>
public readonly record struct State<TState, TValue>(Func<TState, (TValue Value, TState State)> Run)
{
    public (TValue Value, TState State) Invoke(TState state)
        => Run(state);
}

/// <summary>
/// <summary>
/// Reader monad carries an environment through chained computations.
/// </summary>
public readonly record struct Reader<TEnv, TValue>(Func<TEnv, TValue> Run)
{
    public TValue Invoke(TEnv environment) => Run(environment);

    public override string ToString() => $"Reader({typeof(TEnv).Name} -> {typeof(TValue).Name})";
}

/// <summary>
/// Writer monad accumulates logs alongside a value.
/// </summary>
public readonly record struct Writer<TValue, TLog>(TValue Value, IReadOnlyList<TLog> Logs)
{
    public void Deconstruct(out TValue value, out IReadOnlyList<TLog> logs)
    {
        value = Value;
        logs = Logs;
    }

    public override string ToString()
        => $"Writer(Value: {Value}, Logs: [{string.Join(", ", Logs)}])";
}

/// <summary>
/// Continuation monad models computations in continuation-passing style.
/// </summary>
public readonly record struct Cont<TOutput, TValue>(Func<Func<TValue, TOutput>, TOutput> Run)
{
    public TOutput Invoke(Func<TValue, TOutput> continuation)
        => Run(continuation);
}

/// <summary>
/// Validation applicative accumulates errors while computing a value.
/// </summary>
public readonly record struct Validation<TValue>(bool IsValid, TValue? Value, IReadOnlyList<string> Errors)
{
    public static Validation<TValue> Success(TValue value)
        => new(true, value, Array.Empty<string>());

    public static Validation<TValue> Failure(params string[] errors)
        => new(false, default, Array.AsReadOnly(errors));

    public override string ToString()
        => IsValid ? $"Valid({Value})" : $"Invalid([{string.Join(", ", Errors)}])";
}

/// <summary>
/// Lightweight async wrapper that mirrors Haskell's Task/IO monad.
/// </summary>
public readonly record struct TaskIO<T>(Task<T> Task)
{
    public Task<T> Invoke() => Task;

    public static implicit operator TaskIO<T>(Task<T> task)
        => new(task);
}

/// <summary>
/// Helper factory for constructing <see cref="Option{T}"/> instances with concise syntax.
/// </summary>
public static class Option
{
    public static Option<T> Some<T>(T value)
        => Option<T>.Some(value);

    public static Option<T> None<T>()
        => Option<T>.None;

    public static Option<T> FromNullable<T>(T? value)
        where T : class
        => value is null ? Option<T>.None : Option<T>.Some(value);

    public static Option<T> FromNullable<T>(T? value)
        where T : struct
        => value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;
}

/// <summary>
/// Factory helpers for <see cref="Result{T}"/>.
/// </summary>
public static class Result
{
    public static Result<T> Ok<T>(T value)
        => Result<T>.Ok(value);

    public static Result<T> Fail<T>(string error)
        => Result<T>.Fail(error);

    public static Result<T> Try<T>(Func<T> producer)
    {
        try
        {
            return Result<T>.Ok(producer());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex.Message);
        }
    }
}

/// <summary>
/// Factory helpers for <see cref="Try{T}"/>.
/// </summary>
public static class Try
{
    public static Try<T> Run<T>(Func<T> producer)
    {
        try
        {
            return Try<T>.Success(producer());
        }
        catch (Exception ex)
        {
            return Try<T>.Failure(ex);
        }
    }

    public static Try<Unit> Run(Action action)
    {
        try
        {
            action();
            return Try<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Try<Unit>.Failure(ex);
        }
    }
}

/// <summary>
/// Construction helpers for <see cref="IO{T}"/>.
/// </summary>
public static class IO
{
    public static IO<T> From<T>(Func<T> effect)
        => new(effect);

    public static IO<T> Return<T>(T value)
        => new(() => value);

    public static IO<Unit> From(Action action)
        => new(() =>
        {
            action();
            return Unit.Value;
        });
}

/// <summary>
/// Construction helpers for <see cref="State{TState, TValue}"/>.
/// </summary>
public static class State
{
    public static State<TState, TValue> Return<TState, TValue>(TValue value)
        => new(state => (value, state));

    public static State<TState, TState> Get<TState>()
        => new(state => (state, state));

    public static State<TState, Unit> Put<TState>(TState newState)
        => new(_ => (Unit.Value, newState));

    public static State<TState, Unit> Modify<TState>(Func<TState, TState> transformer)
        => new(state => (Unit.Value, transformer(state)));

    public static State<TState, TValue> From<TState, TValue>(Func<TState, (TValue Value, TState State)> runner)
        => new(runner);
}

/// <summary>
/// Construction helpers for <see cref="Reader{TEnv, TValue}"/>.
/// </summary>
public static class Reader
{
    public static Reader<TEnv, TValue> Return<TEnv, TValue>(TValue value)
        => new(_ => value);

    public static Reader<TEnv, TEnv> Ask<TEnv>()
        => new(env => env);

    public static Reader<TEnv, TValue> From<TEnv, TValue>(Func<TEnv, TValue> projection)
        => new(projection);
}

/// <summary>
/// Construction helpers for <see cref="Writer{TValue, TLog}"/>.
/// </summary>
public static class Writer
{
    public static Writer<TValue, TLog> Return<TValue, TLog>(TValue value)
        => new(value, Array.Empty<TLog>());

    public static Writer<Unit, TLog> Tell<TLog>(TLog log)
        => new(Unit.Value, new[] { log });

    public static Writer<TValue, TLog> From<TValue, TLog>(TValue value, params TLog[] logs)
        => new(value, logs);
}

/// <summary>
/// Construction helpers for <see cref="Cont{TOutput, TValue}"/>.
/// </summary>
public static class Continuation
{
    public static Cont<TOutput, TValue> Return<TOutput, TValue>(TValue value)
        => new(continuation => continuation(value));

    public static Cont<TOutput, TValue> From<TOutput, TValue>(Func<Func<TValue, TOutput>, TOutput> runner)
        => new(runner);

    public static Cont<TOutput, TValue> CallCC<TOutput, TValue>(
        Func<Func<TValue, Cont<TOutput, TValue>>, Cont<TOutput, TValue>> function)
        => new(continuation =>
        {
            Cont<TOutput, TValue> Escape(TValue value) => new(_ => continuation(value));
            return function(Escape).Run(continuation);
        });
}

/// <summary>
/// Factory helpers for <see cref="Validation{TValue}"/>.
/// </summary>
public static class Validation
{
    public static Validation<TValue> Success<TValue>(TValue value)
        => Validation<TValue>.Success(value);

    public static Validation<TValue> Failure<TValue>(params string[] errors)
        => Validation<TValue>.Failure(errors);

    public static Validation<TValue> From<TValue>(TValue value, params string[] errors)
        => errors.Length == 0 ? Success(value) : Failure<TValue>(errors);
}

/// <summary>
/// Construction helpers for <see cref="TaskIO{T}"/>.
/// </summary>
public static class TaskIO
{
    public static TaskIO<T> Return<T>(T value)
        => new(System.Threading.Tasks.Task.FromResult(value));

    public static TaskIO<T> From<T>(Func<Task<T>> producer)
        => new(producer());

    public static TaskIO<Unit> From(Func<Task> producer)
        => new(producer().ContinueWith(_ => Unit.Value));
}

/// <summary>
/// Construction helpers for <see cref="TaskResult{T}"/>.
/// </summary>
public static class TaskResults
{
    public static TaskResult<T> Return<T>(T value)
        => new(Task.FromResult(Result<T>.Ok(value)));

    public static TaskResult<T> Fail<T>(string error)
        => new(Task.FromResult(Result<T>.Fail(error)));

    public static TaskResult<T> FromResult<T>(Result<T> result)
        => new(Task.FromResult(result));

    public static TaskResult<T> From<T>(Func<Task<Result<T>>> producer)
        => new(producer());

    public static TaskResult<T> From<T>(Func<Task<T>> producer)
        => new(ExecuteAsync(producer));

    public static TaskResult<T> FromTask<T>(Task<T> task)
        => new(ExecuteAsync(() => task));

    private static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> producer)
    {
        try
        {
            var value = await producer().ConfigureAwait(false);
            return Result<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex.Message);
        }
    }
}

/// <summary>
/// Factory helpers for <see cref="ReaderTaskResult{TEnv, TValue}"/>.
/// </summary>
public static class ReaderTaskResults
{
    public static ReaderTaskResult<TEnv, TValue> Return<TEnv, TValue>(TValue value)
        => new(_ => TaskResults.Return(value));

    public static ReaderTaskResult<TEnv, TEnv> Ask<TEnv>()
        => new(env => TaskResults.Return(env));

    public static ReaderTaskResult<TEnv, TValue> From<TEnv, TValue>(Func<TEnv, TaskResult<TValue>> runner)
        => new(runner);
}

/// <summary>
/// Factory helpers for <see cref="StateTaskResult{TState, TValue}"/>.
/// </summary>
public static class StateTaskResults
{
    public static StateTaskResult<TState, TValue> Return<TState, TValue>(TValue value)
        => new(state => TaskResults.Return((value, state)));

    public static StateTaskResult<TState, TState> Get<TState>()
        => new(state => TaskResults.Return((state, state)));

    public static StateTaskResult<TState, Unit> Put<TState>(TState newState)
        => new(_ => TaskResults.Return((Unit.Value, newState)));

    public static StateTaskResult<TState, Unit> Modify<TState>(Func<TState, TState> transformer)
        => new(state => TaskResults.Return((Unit.Value, transformer(state))));

    public static StateTaskResult<TState, TValue> From<TState, TValue>(Func<TState, TaskResult<(TValue Value, TState State)>> runner)
        => new(runner);
}

/// <summary>
/// Central location for the extension blocks that light up the syntax used in <see cref="ExtensionMembersDemo"/>.
/// </summary>
public static class SequenceExtensions
{
    // Instance-style extensions: all members in this block can be invoked as instance members on IEnumerable<TSource>.
    extension<TSource>(IEnumerable<TSource> source)
    {
        /// <summary>
        /// Property-like syntax that checks whether the sequence is empty. Because the compiler creates the forwarding
        /// logic, the property appears as though it were declared on IEnumerable{TSource}.
        /// </summary>
        public bool IsEmpty => !source.Any();

        /// <summary>
        /// Simple filtering extension method implemented without relying on the existing LINQ Where extension.
        /// </summary>
        public IEnumerable<TSource> Filter(Func<TSource, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Returns the first element as an <see cref="Option{TSource}"/>, avoiding exceptions when the sequence is empty.
        /// </summary>
        public Option<TSource> FirstOption()
        {
            foreach (var item in source)
            {
                return Option<TSource>.Some(item);
            }

            return Option<TSource>.None;
        }

        /// <summary>
        /// Returns the first element matching <paramref name="predicate"/> as an <see cref="Option{TSource}"/>.
        /// </summary>
        public Option<TSource> FirstOption(Func<TSource, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return Option<TSource>.Some(item);
                }
            }

            return Option<TSource>.None;
        }
    }

    // Static-style extensions: these become visible as if they were static members on IEnumerable<TSource>.
    extension<TSource>(IEnumerable<TSource>)
    {
        /// <summary>
        /// Static property exposed on IEnumerable{TSource} that returns an empty sequence.
        /// </summary>
        public static IEnumerable<TSource> Identity => Enumerable.Empty<TSource>();

        /// <summary>
        /// Static helper that combines two sequences without mutating either input.
        /// </summary>
        public static IEnumerable<TSource> Combine(IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            foreach (var item in first)
            {
                yield return item;
            }

            foreach (var item in second)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Static operator that enables piping syntax such as <c>sequenceA | sequenceB</c>.
        /// </summary>
        public static IEnumerable<TSource> operator |(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => Combine(left, right);

        /// <summary>
        /// Produces the set union of two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator +(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Union(right);

        /// <summary>
        /// Appends a single value to the end of the sequence.
        /// </summary>
        public static IEnumerable<TSource> operator +(IEnumerable<TSource> source, TSource value)
            => source.Append(value);

        /// <summary>
        /// Prepends a single value to the beginning of the sequence.
        /// </summary>
        public static IEnumerable<TSource> operator +(TSource value, IEnumerable<TSource> source)
            => Enumerable.Repeat(value, 1).Concat(source);

        /// <summary>
        /// Produces the set difference between two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator -(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Except(right);

        /// <summary>
        /// Produces the set intersection between two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator &(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Intersect(right);

        /// <summary>
        /// Produces the symmetric difference between two sequences using the default equality comparer.
        /// </summary>
        public static IEnumerable<TSource> operator ^(IEnumerable<TSource> left, IEnumerable<TSource> right)
            => left.Except(right).Union(right.Except(left));

        /// <summary>
        /// Reverses the sequence without mutating the original source.
        /// </summary>
        public static IEnumerable<TSource> operator ~(IEnumerable<TSource> source)
            => source.Reverse();

        /// <summary>
        /// Repeats the sequence the specified number of times.
        /// </summary>
        public static IEnumerable<TSource> operator *(IEnumerable<TSource> source, int repetitions)
        {
            if (repetitions < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repetitions));
            }

            return RepeatIterator(source, repetitions);
        }

        private static IEnumerable<TSource> RepeatIterator(IEnumerable<TSource> source, int repetitions)
        {
            var materialized = source as IReadOnlyCollection<TSource> ?? source.ToList();
            for (var i = 0; i < repetitions; i++)
            {
                foreach (var item in materialized)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Materializes the sequence into a <see cref="List{T}"/> using unary <c>!</c> for brevity.
        /// Example: <c>var list = !mySequence;</c>
        /// </summary>
        public static List<TSource> operator !(IEnumerable<TSource> source)
            => source.ToList();

        /// <summary>
        /// Joins the sequence using the provided separator via bitwise-or with a string.
        /// Example: <c>var csv = numbers | ", ";</c>
        /// </summary>
        public static string operator |(IEnumerable<TSource> source, string separator)
            => string.Join(separator, source);

        /// <summary>
        /// Splits the sequence into chunks of <paramref name="size"/> via division syntax.
        /// Example: <c>var chunks = numbers / 3;</c>
        /// </summary>
        public static IEnumerable<IReadOnlyList<TSource>> operator /(IEnumerable<TSource> source, int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return source.Chunk(size).Select(static chunk => (IReadOnlyList<TSource>)chunk.ToArray());
        }

        /// <summary>
        /// Returns the trailing <paramref name="count"/> elements using the modulus operator.
        /// Example: <c>var tail = numbers % 2;</c>
        /// </summary>
        public static IEnumerable<TSource> operator %(IEnumerable<TSource> source, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return source.TakeLast(count);
        }

        /// <summary>
        /// Takes the first <paramref name="count"/> elements using left shift syntax.
        /// Example: <c>var firstThree = numbers << 3;</c>
        /// </summary>
        public static IEnumerable<TSource> operator <<(IEnumerable<TSource> source, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return source.Take(count);
        }

        /// <summary>
        /// Skips the first <paramref name="count"/> elements using right shift syntax.
        /// Example: <c>var tail = numbers >> 2;</c>
        /// </summary>
        public static IEnumerable<TSource> operator >>(IEnumerable<TSource> source, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return source.Skip(count);
        }
    }

    // Generic value piping helpers to enable terse sink invocation such as value | WriteLine.
    extension<TValue>(TValue subject)
    {
        /// <summary>
        /// Writes the value to the console and returns it for potential fluent usage.
        /// </summary>
        public TValue WriteLine()
        {
            System.Console.WriteLine(subject);
            return subject;
        }

        /// <summary>
        /// Pipes the value into the provided <paramref name="sink"/> action before returning the original value.
        /// Enables patterns such as <c>var logged = $"Hello, world" | WriteLine;</c>.
        /// </summary>
        public static TValue operator |(TValue value, Action<TValue> sink)
        {
            sink(value);
            return value;
        }

        /// <summary>
        /// Pipes the value into the provided <paramref name="sink"/> action via <c>&gt;&gt;</c> before returning it.
        /// Enables patterns such as <c>$"Hello, world" &gt;&gt; WriteLine;</c>.
        /// </summary>
        public static TValue operator >>(TValue value, Action<TValue> sink)
            => value | sink;

        /// <summary>
        /// Converts the value into an <see cref="Option{TValue}"/>, treating null references as None.
        /// </summary>
        public Option<TValue> ToOption()
            => subject is null ? Option<TValue>.None : Option<TValue>.Some(subject);

        /// <summary>
        /// Converts the value into an <see cref="Option{TValue}"/> when <paramref name="predicate"/> passes.
        /// </summary>
        public Option<TValue> ToOption(Func<TValue, bool> predicate)
            => predicate(subject) ? Option<TValue>.Some(subject) : Option<TValue>.None;

        /// <summary>
        /// Lifts the value into a successful <see cref="Result{TValue}"/>.
        /// </summary>
        public Result<TValue> ToOk()
            => Result<TValue>.Ok(subject);

        /// <summary>
        /// Validates the value, producing either <c>Ok</c> or <c>Error</c>.
        /// </summary>
        public Result<TValue> Validate(Func<TValue, bool> predicate, Func<TValue, string> errorFactory)
            => predicate(subject) ? Result<TValue>.Ok(subject) : Result<TValue>.Fail(errorFactory(subject));

        /// <summary>
        /// Wraps the value in a pure <see cref="IO{T}"/>.
        /// </summary>
        public IO<TValue> ToIO()
            => IO.Return(subject);

        /// <summary>
        /// Lifts the value into a successful <see cref="TaskResult{T}"/>.
        /// </summary>
        public TaskResult<TValue> ToTaskResult()
            => TaskResults.Return(subject);
    }

    // Haskell-inspired Option helpers.
    extension<T>(Option<T> option)
    {
        /// <summary>
        /// Indicates that the option carries a value.
        /// </summary>
        public bool IsSome => option.HasValue;

        /// <summary>
        /// Indicates that the option is empty.
        /// </summary>
        public bool IsNone => !option.HasValue;

        /// <summary>
        /// Returns the contained value or <paramref name="fallback"/> when empty.
        /// </summary>
        public T ValueOr(T fallback)
            => option.HasValue ? option.Value! : fallback;

        /// <summary>
        /// Returns the contained value or computes one from <paramref name="fallback"/> when empty.
        /// </summary>
        public T ValueOrElse(Func<T> fallback)
            => option.HasValue ? option.Value! : fallback();

        /// <summary>
        /// Functional map operation that lifts <paramref name="selector"/> into the option context.
        /// </summary>
        public Option<TResult> Map<TResult>(Func<T, TResult> selector)
            => option.HasValue ? Option<TResult>.Some(selector(option.Value!)) : Option<TResult>.None;

        /// <summary>
        /// Functional bind (flat-map) operation that chains optional computations.
        /// </summary>
        public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder)
            => option.HasValue ? binder(option.Value!) : Option<TResult>.None;

        /// <summary>
        /// Applies an optional function to the current optional value (<c>&lt;*&gt;</c> in Haskell).
        /// </summary>
        public Option<TResult> Apply<TResult>(Option<Func<T, TResult>> applicative)
            => applicative.HasValue && option.HasValue
                ? Option<TResult>.Some(applicative.Value!(option.Value!))
                : Option<TResult>.None;

        /// <summary>
        /// Filters the option by <paramref name="predicate"/>, producing None when the predicate fails.
        /// </summary>
        public Option<T> Where(Func<T, bool> predicate)
            => option.HasValue && predicate(option.Value!) ? option : Option<T>.None;

        /// <summary>
        /// Returns the current option when populated, otherwise evaluates <paramref name="fallback"/>.
        /// </summary>
        public Option<T> OrElse(Func<Option<T>> fallback)
            => option.HasValue ? option : fallback();

        /// <summary>
        /// Custom LINQ support method that enables query expressions on Option.
        /// </summary>
        public Option<TResult> Select<TResult>(Func<T, TResult> selector)
            => option.Map(selector);

        /// <summary>
        /// Custom LINQ support method that enables query expressions on Option.
        /// </summary>
        public Option<TResult> SelectMany<TResult>(Func<T, Option<TResult>> binder)
            => option.Bind(binder);

        /// <summary>
        /// Custom LINQ support method that enables query expressions with projections.
        /// </summary>
        public Option<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Option<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            if (!option.HasValue)
            {
                return Option<TResult>.None;
            }

            var intermediate = binder(option.Value!);
            return intermediate.HasValue
                ? Option<TResult>.Some(projector(option.Value!, intermediate.Value!))
                : Option<TResult>.None;
        }

        /// <summary>
        /// Alternative choice operator (<c>&lt;|&gt;</c> in Haskell).
        /// </summary>
        public static Option<T> operator |(Option<T> first, Option<T> second)
            => first.HasValue ? first : second;

        /// <summary>
        /// Equality-friendly match helper that pipes an option into pattern-style handlers.
        /// </summary>
        public TResult Match<TResult>(Func<T, TResult> whenSome, Func<TResult> whenNone)
            => option.HasValue ? whenSome(option.Value!) : whenNone();

        /// <summary>
        /// Converts the option into a <see cref="Result{T}"/>, using <paramref name="error"/> when empty.
        /// </summary>
        public Result<T> ToResult(string error)
            => option.HasValue ? Result<T>.Ok(option.Value!) : Result<T>.Fail(error);

        /// <summary>
        /// Converts the option into a lazy IO action that throws when the option is empty.
        /// </summary>
        public IO<T> ToIO(Func<string>? errorFactory = null)
            => IO.From(() =>
            {
                if (option.HasValue)
                {
                    return option.Value!;
                }

                throw new InvalidOperationException(errorFactory?.Invoke() ?? "Option had no value.");
            });

        /// <summary>
        /// Converts the option into a <see cref="Try{T}"/>, using <paramref name="errorFactory"/> to build the exception when empty.
        /// </summary>
        public Try<T> ToTry(Func<string>? errorFactory = null)
            => option.HasValue
                ? Try<T>.Success(option.Value!)
                : Try<T>.Failure(new InvalidOperationException(errorFactory?.Invoke() ?? "Option had no value."));

        /// <summary>
        /// Converts the option into a <see cref="TaskResult{T}"/>, using <paramref name="error"/> when empty.
        /// </summary>
        public TaskResult<T> ToTaskResult(string error)
            => option.HasValue ? TaskResults.Return(option.Value!) : TaskResults.Fail<T>(error);

        /// <summary>
        /// Converts the option into a <see cref="TaskResult{T}"/>, using <paramref name="errorFactory"/> when empty.
        /// </summary>
        public TaskResult<T> ToTaskResult(Func<string> errorFactory)
            => option.HasValue ? TaskResults.Return(option.Value!) : TaskResults.Fail<T>(errorFactory());
    }

    // Applicative operators riding on Option<Func<...>> instances.
    extension<TArg, TResult>(Option<Func<TArg, TResult>> applicative)
    {
        /// <summary>
        /// Applies an optional function to an optional argument, mirroring Haskell's <c>&lt;*&gt;</c>.
        /// </summary>
        public static Option<TResult> operator *(Option<Func<TArg, TResult>> function, Option<TArg> value)
            => function.HasValue && value.HasValue
                ? Option<TResult>.Some(function.Value!(value.Value!))
                : Option<TResult>.None;
    }

    extension<TArg1, TArg2, TResult>(Option<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        /// <summary>
        /// Supports multi-argument applicative style by partially applying the optional function.
        /// </summary>
        public static Option<Func<TArg2, TResult>> operator *(Option<Func<TArg1, Func<TArg2, TResult>>> function, Option<TArg1> value)
            => function.HasValue && value.HasValue
                ? Option<Func<TArg2, TResult>>.Some(function.Value!(value.Value!))
                : Option<Func<TArg2, TResult>>.None;
    }

    // Result monad helpers.
    extension<T>(Result<T> result)
    {
        public bool IsOk => result.IsSuccess;
        public bool IsError => !result.IsSuccess;
        public string? Error => result.Error;

        public T ValueOr(T fallback)
            => result.IsSuccess ? result.Value! : fallback;

        public T ValueOrElse(Func<string?, T> fallback)
            => result.IsSuccess ? result.Value! : fallback(result.Error);

        public Result<TResult> Map<TResult>(Func<T, TResult> selector)
            => result.IsSuccess
                ? Result<TResult>.Ok(selector(result.Value!))
                : Result<TResult>.Fail(result.Error ?? "Unknown error");

        public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
            => result.IsSuccess
                ? binder(result.Value!)
                : Result<TResult>.Fail(result.Error ?? "Unknown error");

        public Result<T> Tap(Action<T> inspector)
        {
            if (result.IsSuccess)
            {
                inspector(result.Value!);
            }

            return result;
        }

        public Result<T> Recover(Func<string?, T> recover)
            => result.IsSuccess ? result : Result<T>.Ok(recover(result.Error));

        public Result<T> RecoverWith(Func<string?, Result<T>> recover)
            => result.IsSuccess ? result : recover(result.Error);

        public Result<T> OrElse(Func<Result<T>> fallback)
            => result.IsSuccess ? result : fallback();

        public TResult Match<TResult>(Func<T, TResult> onOk, Func<string?, TResult> onError)
            => result.IsSuccess ? onOk(result.Value!) : onError(result.Error);

        public Result<TResult> Select<TResult>(Func<T, TResult> selector)
            => result.Map(selector);

        public Result<TResult> SelectMany<TResult>(Func<T, Result<TResult>> binder)
            => result.Bind(binder);

        public Result<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Result<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            if (!result.IsSuccess)
            {
                return Result<TResult>.Fail(result.Error ?? "Unknown error");
            }

            var intermediate = binder(result.Value!);
            return intermediate.IsSuccess
                ? Result<TResult>.Ok(projector(result.Value!, intermediate.Value!))
                : Result<TResult>.Fail(intermediate.Error ?? "Unknown error");
        }

        public Result<TResult> Apply<TResult>(Result<Func<T, TResult>> applicative)
            => result.IsSuccess && applicative.IsSuccess
                ? Result<TResult>.Ok(applicative.Value!(result.Value!))
                : Result<TResult>.Fail(applicative.Error ?? result.Error ?? "Unknown error");

        public static Result<T> operator |(Result<T> first, Result<T> second)
            => first.IsSuccess ? first : second;

        /// <summary>
        /// Converts the result into an option, erasing the error information.
        /// </summary>
        public Option<T> ToOption()
            => result.IsSuccess ? Option<T>.Some(result.Value!) : Option<T>.None;

        /// <summary>
        /// Converts the result into a lazy IO action that throws when the result is an error.
        /// </summary>
        public IO<T> ToIO()
            => IO.From(() =>
            {
                if (result.IsSuccess)
                {
                    return result.Value!;
                }

                throw new InvalidOperationException(result.Error ?? "Unknown error");
            });

        /// <summary>
        /// Converts the result into a <see cref="Try{T}"/>, turning the error into an exception.
        /// </summary>
        public Try<T> ToTry()
            => result.IsSuccess
                ? Try<T>.Success(result.Value!)
                : Try<T>.Failure(new InvalidOperationException(result.Error ?? "Unknown error"));

        /// <summary>
        /// Wraps the result in a completed <see cref="TaskResult{T}"/>.
        /// </summary>
        public TaskResult<T> ToTaskResult()
            => TaskResults.FromResult(result);
    }

    extension<TArg, TResult>(Result<Func<TArg, TResult>> applicative)
    {
        public static Result<TResult> operator *(Result<Func<TArg, TResult>> function, Result<TArg> value)
            => function.IsSuccess && value.IsSuccess
                ? Result<TResult>.Ok(function.Value!(value.Value!))
                : Result<TResult>.Fail(function.Error ?? value.Error ?? "Unknown error");
    }

    extension<TArg1, TArg2, TResult>(Result<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        public static Result<Func<TArg2, TResult>> operator *(Result<Func<TArg1, Func<TArg2, TResult>>> function, Result<TArg1> value)
            => function.IsSuccess && value.IsSuccess
                ? Result<Func<TArg2, TResult>>.Ok(function.Value!(value.Value!))
                : Result<Func<TArg2, TResult>>.Fail(function.Error ?? value.Error ?? "Unknown error");
    }

    // Try monad helpers.
    extension<T>(Try<T> attempt)
    {
        public bool IsSuccess => attempt.IsSuccess;
        public bool IsFailure => !attempt.IsSuccess;
        public Exception? Exception => attempt.Exception;

        public T GetOrThrow()
            => attempt.GetOrThrow();

        public Try<TResult> Map<TResult>(Func<T, TResult> selector)
            => attempt.IsSuccess
                ? Try<TResult>.Success(selector(attempt.Value!))
                : Try<TResult>.Failure(attempt.Exception ?? new InvalidOperationException("Unknown error"));

        public Try<TResult> Bind<TResult>(Func<T, Try<TResult>> binder)
            => attempt.IsSuccess
                ? binder(attempt.Value!)
                : Try<TResult>.Failure(attempt.Exception ?? new InvalidOperationException("Unknown error"));

        public Try<T> Recover(Func<Exception?, T> recover)
            => attempt.IsSuccess ? attempt : Try<T>.Success(recover(attempt.Exception));

        public Try<T> RecoverWith(Func<Exception?, Try<T>> recover)
            => attempt.IsSuccess ? attempt : recover(attempt.Exception);

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception?, TResult> onFailure)
            => attempt.IsSuccess ? onSuccess(attempt.Value!) : onFailure(attempt.Exception);

        public Result<T> ToResult()
            => attempt.IsSuccess
                ? Result<T>.Ok(attempt.Value!)
                : Result<T>.Fail(attempt.Exception?.Message ?? "Unknown error");

        public Option<T> ToOption()
            => attempt.IsSuccess ? Option<T>.Some(attempt.Value!) : Option<T>.None;

        public IO<T> ToIO()
            => IO.From(attempt.GetOrThrow);

        public TaskResult<T> ToTaskResult()
            => TaskResults.FromResult(attempt.ToResult());

        public Try<TResult> Select<TResult>(Func<T, TResult> selector)
            => attempt.Map(selector);

        public Try<TResult> SelectMany<TResult>(Func<T, Try<TResult>> binder)
            => attempt.Bind(binder);

        public Try<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, Try<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
        {
            if (!attempt.IsSuccess)
            {
                return Try<TResult>.Failure(attempt.Exception ?? new InvalidOperationException("Unknown error"));
            }

            var intermediate = binder(attempt.Value!);
            return intermediate.IsSuccess
                ? Try<TResult>.Success(projector(attempt.Value!, intermediate.Value!))
                : Try<TResult>.Failure(intermediate.Exception ?? new InvalidOperationException("Unknown error"));
        }

        public Try<TResult> Apply<TResult>(Try<Func<T, TResult>> applicative)
            => attempt.IsSuccess && applicative.IsSuccess
                ? Try<TResult>.Success(applicative.Value!(attempt.Value!))
                : Try<TResult>.Failure(applicative.Exception ?? attempt.Exception ?? new InvalidOperationException("Unknown error"));

        public static Try<T> operator |(Try<T> first, Try<T> second)
            => first.IsSuccess ? first : second;
    }

    extension<TArg, TResult>(Try<Func<TArg, TResult>> applicative)
    {
        public static Try<TResult> operator *(Try<Func<TArg, TResult>> function, Try<TArg> value)
            => function.IsSuccess && value.IsSuccess
                ? Try<TResult>.Success(function.Value!(value.Value!))
                : Try<TResult>.Failure(function.Exception ?? value.Exception ?? new InvalidOperationException("Unknown error"));
    }

    extension<TArg1, TArg2, TResult>(Try<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        public static Try<Func<TArg2, TResult>> operator *(Try<Func<TArg1, Func<TArg2, TResult>>> function, Try<TArg1> value)
            => function.IsSuccess && value.IsSuccess
                ? Try<Func<TArg2, TResult>>.Success(function.Value!(value.Value!))
                : Try<Func<TArg2, TResult>>.Failure(function.Exception ?? value.Exception ?? new InvalidOperationException("Unknown error"));
    }

    // State monad helpers.
    extension<TState, TValue>(State<TState, TValue> stateMonad)
    {
        public (TValue Value, TState State) RunState(TState initialState)
            => stateMonad.Invoke(initialState);

        public TValue Evaluate(TState initialState)
            => stateMonad.Invoke(initialState).Value;

        public TState Execute(TState initialState)
            => stateMonad.Invoke(initialState).State;

        public State<TState, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(state =>
            {
                var (value, nextState) = stateMonad.Invoke(state);
                return (selector(value), nextState);
            });

        public State<TState, TResult> Bind<TResult>(Func<TValue, State<TState, TResult>> binder)
            => new(state =>
            {
                var (value, nextState) = stateMonad.Invoke(state);
                return binder(value).Invoke(nextState);
            });

        public State<TState, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => stateMonad.Map(selector);

        public State<TState, TResult> SelectMany<TResult>(Func<TValue, State<TState, TResult>> binder)
            => stateMonad.Bind(binder);

        public State<TState, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, State<TState, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(state =>
            {
                var (value, firstState) = stateMonad.Invoke(state);
                var (intermediate, secondState) = binder(value).Invoke(firstState);
                return (projector(value, intermediate), secondState);
            });

        public State<TState, TResult> Apply<TResult>(State<TState, Func<TValue, TResult>> applicative)
            => new(state =>
            {
                var (func, firstState) = applicative.Invoke(state);
                var (value, secondState) = stateMonad.Invoke(firstState);
                return (func(value), secondState);
            });

        public IO<TValue> ToIO(TState initialState)
            => IO.From(() => stateMonad.Invoke(initialState).Value);

        public Result<TValue> ToResult(TState initialState)
        {
            try
            {
                return Result<TValue>.Ok(stateMonad.Invoke(initialState).Value);
            }
            catch (Exception ex)
            {
                return Result<TValue>.Fail(ex.Message);
            }
        }
    }

    extension<TState, TArg, TResult>(State<TState, Func<TArg, TResult>> applicative)
    {
        public static State<TState, TResult> operator *(State<TState, Func<TArg, TResult>> function, State<TState, TArg> value)
            => new(state =>
            {
                var (func, state1) = function.Invoke(state);
                var (arg, state2) = value.Invoke(state1);
                return (func(arg), state2);
            });
    }

    // TaskResult monad helpers.
    extension<T>(TaskResult<T> taskResult)
    {
        public Task<Result<T>> RunAsync()
            => taskResult.Invoke();

        public TaskResult<TResult> Map<TResult>(Func<T, TResult> selector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                return result.Map(selector);
            });

        public TaskResult<TResult> Bind<TResult>(Func<T, TaskResult<TResult>> binder)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return Result<TResult>.Fail(result.Error ?? "Unknown error");
                }

                return await binder(result.Value!).Invoke().ConfigureAwait(false);
            });

        public TaskResult<TResult> Apply<TResult>(TaskResult<Func<T, TResult>> applicative)
            => TaskResults.From(async () =>
            {
                var funcResult = await applicative.Invoke().ConfigureAwait(false);
                var valueResult = await taskResult.Invoke().ConfigureAwait(false);

                if (!funcResult.IsSuccess)
                {
                    return Result<TResult>.Fail(funcResult.Error ?? "Unknown error");
                }

                if (!valueResult.IsSuccess)
                {
                    return Result<TResult>.Fail(valueResult.Error ?? "Unknown error");
                }

                return Result<TResult>.Ok(funcResult.Value!(valueResult.Value!));
            });

        public TaskResult<T> Tap(Action<T> inspector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    inspector(result.Value!);
                }

                return result;
            });

        public TaskResult<T> Tap(Func<T, Task> inspector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await inspector(result.Value!).ConfigureAwait(false);
                }

                return result;
            });

        public TaskResult<T> OrElse(Func<TaskResult<T>> fallback)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                return result.IsSuccess ? result : await fallback().Invoke().ConfigureAwait(false);
            });

        public TaskResult<TResult> Select<TResult>(Func<T, TResult> selector)
            => taskResult.Map(selector);

        public TaskResult<TResult> SelectMany<TResult>(Func<T, TaskResult<TResult>> binder)
            => taskResult.Bind(binder);

        public TaskResult<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, TaskResult<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
            => TaskResults.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return Result<TResult>.Fail(result.Error ?? "Unknown error");
                }

                var intermediate = await binder(result.Value!).Invoke().ConfigureAwait(false);
                if (!intermediate.IsSuccess)
                {
                    return Result<TResult>.Fail(intermediate.Error ?? "Unknown error");
                }

                return Result<TResult>.Ok(projector(result.Value!, intermediate.Value!));
            });

        public Task<Option<T>> ToOptionAsync()
            => taskResult.Invoke().ContinueWith(static t => t.Result.IsSuccess ? Option<T>.Some(t.Result.Value!) : Option<T>.None, TaskContinuationOptions.ExecuteSynchronously);

        public Task<Result<T>> ToResultAsync()
            => taskResult.Invoke();

        public TaskResult<T> Ensure(Func<T, bool> predicate, string error)
            => TaskResults.From(async () =>
            {
                var current = await taskResult.Invoke().ConfigureAwait(false);
                return current.IsSuccess && predicate(current.Value!)
                    ? current
                    : Result<T>.Fail(error);
            });

        public TaskIO<T> ToTaskIO(Func<string?, Exception>? errorFactory = null)
            => TaskIO.From(async () =>
            {
                var result = await taskResult.Invoke().ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    return result.Value!;
                }

                throw errorFactory?.Invoke(result.Error) ?? new InvalidOperationException(result.Error ?? "Unknown error");
            });
    }

    // IO monad helpers.
    extension<T>(IO<T> io)
    {
        public T Run() => io.Invoke();

        public IO<TResult> Map<TResult>(Func<T, TResult> selector)
            => new(() => selector(io.Invoke()));

        public IO<TResult> Bind<TResult>(Func<T, IO<TResult>> binder)
            => new(() =>
            {
                var value = io.Invoke();
                return binder(value).Invoke();
            });

        public IO<T> Tap(Action<T> inspector)
            => new(() =>
            {
                var value = io.Invoke();
                inspector(value);
                return value;
            });

        public IO<TResult> Apply<TResult>(IO<Func<T, TResult>> applicative)
            => new(() =>
            {
                var func = applicative.Invoke();
                var value = io.Invoke();
                return func(value);
            });

        public IO<TResult> Select<TResult>(Func<T, TResult> selector)
            => io.Map(selector);

        public IO<TResult> SelectMany<TResult>(Func<T, IO<TResult>> binder)
            => io.Bind(binder);

        public IO<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, IO<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
            => new(() =>
            {
                var value = io.Invoke();
                var intermediate = binder(value).Invoke();
                return projector(value, intermediate);
            });

        public IO<TResult> Then<TResult>(IO<TResult> next)
            => new(() =>
            {
                _ = io.Invoke();
                return next.Invoke();
            });

        public Result<T> ToResult()
        {
            try
            {
                return Result<T>.Ok(io.Invoke());
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(ex.Message);
            }
        }

        public Option<T> ToOption()
        {
            try
            {
                var value = io.Invoke();
                return value is null ? Option<T>.None : Option<T>.Some(value);
            }
            catch
            {
                return Option<T>.None;
            }
        }

        public Try<T> ToTry()
            => Try.Run(io.Invoke);

        public TaskResult<T> ToTaskResult(Func<Exception, string>? errorFactory = null)
            => TaskResults.From(async () =>
            {
                try
                {
                    var value = io.Invoke();
                    return Result<T>.Ok(value);
                }
                catch (Exception ex)
                {
                    return Result<T>.Fail(errorFactory?.Invoke(ex) ?? ex.Message);
                }
            });
    }

    extension<TArg, TResult>(IO<Func<TArg, TResult>> applicative)
    {
        public static IO<TResult> operator *(IO<Func<TArg, TResult>> function, IO<TArg> value)
            => new(() =>
            {
                var func = function.Invoke();
                var arg = value.Invoke();
                return func(arg);
            });
    }

    extension<TArg1, TArg2, TResult>(IO<Func<TArg1, Func<TArg2, TResult>>> applicative)
    {
        public static IO<Func<TArg2, TResult>> operator *(IO<Func<TArg1, Func<TArg2, TResult>>> function, IO<TArg1> value)
            => new(() =>
            {
                var func = function.Invoke();
                var arg = value.Invoke();
                return func(arg);
            });
    }

    extension<T>(Func<T> effect)
    {
        public IO<T> ToIO()
            => IO.From(effect);

        public Result<T> ToResult()
            => Result.Try(effect);

        public Try<T> ToTry()
            => Try.Run(effect);
    }

    extension(Action action)
    {
        public IO<Unit> ToIO()
            => IO.From(action);

        public Result<Unit> ToResult()
            => Result.Try(() =>
            {
                action();
                return Unit.Value;
            });

        public Try<Unit> ToTry()
            => Try.Run(action);
    }

    // Reader monad helpers.
    extension<TEnv, TValue>(Reader<TEnv, TValue> reader)
    {
        public TValue Run(TEnv environment)
            => reader.Invoke(environment);

        public Reader<TEnv, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(env => selector(reader.Invoke(env)));

        public Reader<TEnv, TResult> Bind<TResult>(Func<TValue, Reader<TEnv, TResult>> binder)
            => new(env => binder(reader.Invoke(env)).Invoke(env));

        public Reader<TEnv, TValue> Local(Func<TEnv, TEnv> transformer)
            => new(env => reader.Invoke(transformer(env)));

        public Reader<TEnv, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => reader.Map(selector);

        public Reader<TEnv, TResult> SelectMany<TResult>(Func<TValue, Reader<TEnv, TResult>> binder)
            => reader.Bind(binder);

        public Reader<TEnv, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, Reader<TEnv, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(env =>
            {
                var value = reader.Invoke(env);
                var intermediate = binder(value).Invoke(env);
                return projector(value, intermediate);
            });

        public Reader<TEnv, TResult> Apply<TResult>(Reader<TEnv, Func<TValue, TResult>> applicative)
            => new(env =>
            {
                var func = applicative.Invoke(env);
                var value = reader.Invoke(env);
                return func(value);
            });

        public Func<TEnv, TValue> ToFunc()
            => reader.Run;
    }

    extension<TEnv, TArg, TResult>(Reader<TEnv, Func<TArg, TResult>> applicative)
    {
        public static Reader<TEnv, TResult> operator *(Reader<TEnv, Func<TArg, TResult>> function, Reader<TEnv, TArg> value)
            => new(env =>
            {
                var func = function.Invoke(env);
                var arg = value.Invoke(env);
                return func(arg);
            });
    }

    // ReaderTaskResult helpers.
    extension<TEnv, TValue>(ReaderTaskResult<TEnv, TValue> reader)
    {
        public Task<Result<TValue>> RunAsync(TEnv environment)
            => reader.Invoke(environment).Invoke();

        public ReaderTaskResult<TEnv, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(env => reader.Invoke(env).Map(selector));

        public ReaderTaskResult<TEnv, TResult> Bind<TResult>(Func<TValue, ReaderTaskResult<TEnv, TResult>> binder)
            => new(env => reader.Invoke(env).Bind(value => binder(value).Invoke(env)));

        public ReaderTaskResult<TEnv, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => reader.Map(selector);

        public ReaderTaskResult<TEnv, TResult> SelectMany<TResult>(Func<TValue, ReaderTaskResult<TEnv, TResult>> binder)
            => reader.Bind(binder);

        public ReaderTaskResult<TEnv, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, ReaderTaskResult<TEnv, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(env =>
                reader.Invoke(env).Bind(value =>
                    binder(value).Invoke(env).Map(intermediate => projector(value, intermediate))));

        public ReaderTaskResult<TEnv, TValue> Local(Func<TEnv, TEnv> transformer)
            => new(env => reader.Invoke(transformer(env)));

        public ReaderTaskResult<TEnv, TResult> Apply<TResult>(ReaderTaskResult<TEnv, Func<TValue, TResult>> applicative)
            => new(env =>
                applicative.Invoke(env).Bind(func =>
                    reader.Invoke(env).Map(value => func(value))));

        public TaskResult<TValue> ToTaskResult(TEnv environment)
            => reader.Invoke(environment);
    }

    extension<TEnv, TArg, TResult>(ReaderTaskResult<TEnv, Func<TArg, TResult>> applicative)
    {
        public static ReaderTaskResult<TEnv, TResult> operator *(ReaderTaskResult<TEnv, Func<TArg, TResult>> function, ReaderTaskResult<TEnv, TArg> value)
            => new(env =>
                function.Invoke(env).Bind(func =>
                    value.Invoke(env).Map(arg => func(arg))));
    }

    // Writer monad helpers.
    extension<TValue, TLog>(Writer<TValue, TLog> writer)
    {
        public Writer<TResult, TLog> Map<TResult>(Func<TValue, TResult> selector)
            => new(selector(writer.Value), writer.Logs);

        public Writer<TResult, TLog> Bind<TResult>(Func<TValue, Writer<TResult, TLog>> binder)
        {
            var next = binder(writer.Value);
            return new(next.Value, CombineLogs(writer.Logs, next.Logs));
        }

        public Writer<TValue, TLog> AppendLog(TLog log)
            => new(writer.Value, CombineLogs(writer.Logs, new[] { log }));

        public Writer<TValue, TLog> AppendLogs(params TLog[] logs)
            => new(writer.Value, CombineLogs(writer.Logs, logs));

        public Writer<TValue, TLog> Tap(Action<TValue> inspector)
        {
            inspector(writer.Value);
            return writer;
        }

        public Writer<TValue, TLog> TapLogs(Action<IReadOnlyList<TLog>> inspector)
        {
            inspector(writer.Logs);
            return writer;
        }

        public Writer<TResult, TLog> Select<TResult>(Func<TValue, TResult> selector)
            => writer.Map(selector);

        public Writer<TResult, TLog> SelectMany<TResult>(Func<TValue, Writer<TResult, TLog>> binder)
            => writer.Bind(binder);

        public Writer<TResult, TLog> SelectMany<TIntermediate, TResult>(
            Func<TValue, Writer<TIntermediate, TLog>> binder,
            Func<TValue, TIntermediate, TResult> projector)
        {
            var next = binder(writer.Value);
            var projected = projector(writer.Value, next.Value);
            return new(projected, CombineLogs(writer.Logs, next.Logs));
        }

        public Writer<TResult, TLog> Apply<TResult>(Writer<Func<TValue, TResult>, TLog> applicative)
        {
            var value = applicative.Value(writer.Value);
            return new(value, CombineLogs(applicative.Logs, writer.Logs));
        }

        public IO<TValue> ToIO(Action<TLog>? sink = null)
            => IO.From(() =>
            {
                if (sink is not null)
                {
                    foreach (var log in writer.Logs)
                    {
                        sink(log);
                    }
                }

                return writer.Value;
            });
    }

    extension<TValue, TResult, TLog>(Writer<Func<TValue, TResult>, TLog> applicative)
    {
        public static Writer<TResult, TLog> operator *(Writer<Func<TValue, TResult>, TLog> function, Writer<TValue, TLog> value)
            => new(function.Value(value.Value), CombineLogs(function.Logs, value.Logs));
    }

    extension<TValue>(Writer<TValue, string> writer)
    {
        public string PrettyPrint()
            => $"{writer.Value} | logs: [{string.Join(", ", writer.Logs)}]";
    }

    private static IReadOnlyList<TLog> CombineLogs<TLog>(IReadOnlyList<TLog> first, IReadOnlyList<TLog> second)
    {
        if (first.Count == 0 && second.Count == 0)
        {
            return Array.Empty<TLog>();
        }

        if (first.Count == 0)
        {
            return second;
        }

        if (second.Count == 0)
        {
            return first;
        }

        var combined = new TLog[first.Count + second.Count];
        for (var i = 0; i < first.Count; i++)
        {
            combined[i] = first[i];
        }

        for (var i = 0; i < second.Count; i++)
        {
            combined[first.Count + i] = second[i];
        }

        return Array.AsReadOnly(combined);
    }

    // Continuation monad helpers.
    extension<TOutput, TValue>(Cont<TOutput, TValue> continuationMonad)
    {
        public TOutput Run(Func<TValue, TOutput> continuation)
            => continuationMonad.Invoke(continuation);

        public Cont<TOutput, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(k => continuationMonad.Invoke(value => k(selector(value))));

        public Cont<TOutput, TResult> Bind<TResult>(Func<TValue, Cont<TOutput, TResult>> binder)
            => new(k => continuationMonad.Invoke(value => binder(value).Run(k)));

        public Cont<TOutput, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => continuationMonad.Map(selector);

        public Cont<TOutput, TResult> SelectMany<TResult>(Func<TValue, Cont<TOutput, TResult>> binder)
            => continuationMonad.Bind(binder);

        public Cont<TOutput, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, Cont<TOutput, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(k => continuationMonad.Invoke(value =>
                binder(value).Run(intermediate => k(projector(value, intermediate)))));

        public Cont<TOutput, TResult> Apply<TResult>(Cont<TOutput, Func<TValue, TResult>> applicative)
            => new(k => applicative.Run(func => continuationMonad.Run(value => k(func(value)))));

        public Cont<TOutput, TResult> Then<TResult>(Cont<TOutput, TResult> next)
            => new(k => continuationMonad.Run(_ => next.Run(k)));

        public IO<TOutput> ToIO(Func<TValue, TOutput> finalContinuation)
            => IO.From(() => continuationMonad.Run(finalContinuation));

        public Result<TOutput> ToResult(Func<TValue, TOutput> finalContinuation)
        {
            try
            {
                return Result<TOutput>.Ok(continuationMonad.Run(finalContinuation));
            }
            catch (Exception ex)
            {
                return Result<TOutput>.Fail(ex.Message);
            }
        }
    }

    extension<TOutput, TArg, TResult>(Cont<TOutput, Func<TArg, TResult>> applicative)
    {
        public static Cont<TOutput, TResult> operator *(Cont<TOutput, Func<TArg, TResult>> function, Cont<TOutput, TArg> value)
            => new(k => function.Run(func => value.Run(arg => k(func(arg)))));
    }

    // StateTaskResult helpers.
    extension<TState, TValue>(StateTaskResult<TState, TValue> stateMonad)
    {
        public Task<Result<(TValue Value, TState State)>> RunAsync(TState initialState)
            => stateMonad.Invoke(initialState).Invoke();

        public StateTaskResult<TState, TResult> Map<TResult>(Func<TValue, TResult> selector)
            => new(state => stateMonad.Invoke(state).Map(tuple => (selector(tuple.Value), tuple.State)));

        public StateTaskResult<TState, TResult> Bind<TResult>(Func<TValue, StateTaskResult<TState, TResult>> binder)
            => new(state =>
                stateMonad.Invoke(state).Bind(tuple => binder(tuple.Value).Invoke(tuple.State)));

        public StateTaskResult<TState, TResult> Select<TResult>(Func<TValue, TResult> selector)
            => stateMonad.Map(selector);

        public StateTaskResult<TState, TResult> SelectMany<TResult>(Func<TValue, StateTaskResult<TState, TResult>> binder)
            => stateMonad.Bind(binder);

        public StateTaskResult<TState, TResult> SelectMany<TIntermediate, TResult>(
            Func<TValue, StateTaskResult<TState, TIntermediate>> binder,
            Func<TValue, TIntermediate, TResult> projector)
            => new(state =>
                stateMonad.Invoke(state).Bind(tuple =>
                    binder(tuple.Value).Invoke(tuple.State).Map(inner =>
                        (projector(tuple.Value, inner.Value), inner.State))));

        public StateTaskResult<TState, TResult> Apply<TResult>(StateTaskResult<TState, Func<TValue, TResult>> applicative)
            => new(state =>
                applicative.Invoke(state).Bind(funcTuple =>
                    stateMonad.Invoke(funcTuple.State).Map(valueTuple =>
                        (funcTuple.Value(valueTuple.Value), valueTuple.State))));

        public TaskResult<TValue> Evaluate(TState initialState)
            => stateMonad.Invoke(initialState).Map(tuple => tuple.Value);

        public TaskResult<TState> Execute(TState initialState)
            => stateMonad.Invoke(initialState).Map(tuple => tuple.State);

        public TaskResult<(TValue Value, TState State)> ToTaskResult(TState initialState)
            => stateMonad.Invoke(initialState);
    }

    extension<TState, TArg, TResult>(StateTaskResult<TState, Func<TArg, TResult>> applicative)
    {
        public static StateTaskResult<TState, TResult> operator *(StateTaskResult<TState, Func<TArg, TResult>> function, StateTaskResult<TState, TArg> value)
            => new(state =>
                function.Invoke(state).Bind(funcTuple =>
                    value.Invoke(funcTuple.State).Map(valueTuple =>
                        (funcTuple.Value(valueTuple.Value), valueTuple.State))));
    }

    // Validation applicative helpers.
    extension<TValue>(Validation<TValue> validation)
    {
        public bool IsValid => validation.IsValid;
        public IReadOnlyList<string> Errors => validation.Errors;

        public Validation<TResult> Map<TResult>(Func<TValue, TResult> selector)
            => validation.IsValid
                ? Validation<TResult>.Success(selector(validation.Value!))
                : Validation<TResult>.Failure(validation.Errors.ToArray());

        public Validation<TResult> Apply<TResult>(Validation<Func<TValue, TResult>> applicative)
        {
            if (validation.IsValid && applicative.IsValid)
            {
                return Validation<TResult>.Success(applicative.Value!(validation.Value!));
            }

            return Validation<TResult>.Failure(CombineLogs(applicative.Errors, validation.Errors).ToArray());
        }

        public Validation<TValue> Combine(Validation<TValue> other, Func<TValue, TValue, TValue> combiner)
        {
            if (validation.IsValid && other.IsValid)
            {
                return Validation<TValue>.Success(combiner(validation.Value!, other.Value!));
            }

            return Validation<TValue>.Failure(CombineLogs(validation.Errors, other.Errors).ToArray());
        }

        public Validation<TResult> Select<TResult>(Func<TValue, TResult> selector)
            => validation.IsValid
                ? Validation<TResult>.Success(selector(validation.Value!))
                : Validation<TResult>.Failure(validation.Errors.ToArray());

        public Validation<TValue> Ensure(Func<TValue, bool> requirement, string error)
            => validation.IsValid && requirement(validation.Value!)
                ? validation
                : Validation<TValue>.Failure(CombineLogs(validation.Errors, new[] { error }).ToArray());

        public Validation<TValue> Ensure(Func<TValue, bool> requirement, Func<string> errorFactory)
            => validation.IsValid && requirement(validation.Value!)
                ? validation
                : Validation<TValue>.Failure(CombineLogs(validation.Errors, new[] { errorFactory() }).ToArray());

        public Option<TValue> ToOption()
            => validation.IsValid ? Option<TValue>.Some(validation.Value!) : Option<TValue>.None;

        public Result<TValue> ToResult(string? errorMessage = null)
            => validation.IsValid
                ? Result<TValue>.Ok(validation.Value!)
                : Result<TValue>.Fail(errorMessage ?? string.Join(", ", validation.Errors));

        public TaskResult<TValue> ToTaskResult(string? errorMessage = null)
            => validation.IsValid
                ? TaskResults.Return(validation.Value!)
                : TaskResults.Fail<TValue>(errorMessage ?? string.Join(", ", validation.Errors));
    }

    extension<TValue, TResult>(Validation<Func<TValue, TResult>> applicative)
    {
        public static Validation<TResult> operator *(Validation<Func<TValue, TResult>> function, Validation<TValue> value)
            => value.Apply(function);
    }

    // TaskIO (async) helpers.
    extension<T>(TaskIO<T> taskIO)
    {
        public Task<T> RunAsync()
            => taskIO.Invoke();

        public TaskIO<TResult> Map<TResult>(Func<T, TResult> selector)
            => TaskIO.From(async () => selector(await taskIO.Invoke().ConfigureAwait(false)));

        public TaskIO<TResult> Bind<TResult>(Func<T, TaskIO<TResult>> binder)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                return await binder(value).Invoke().ConfigureAwait(false);
            });

        public TaskIO<T> Tap(Func<T, Task> inspector)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                await inspector(value).ConfigureAwait(false);
                return value;
            });

        public TaskIO<TResult> Apply<TResult>(TaskIO<Func<T, TResult>> applicative)
            => TaskIO.From(async () =>
            {
                var func = await applicative.Invoke().ConfigureAwait(false);
                var value = await taskIO.Invoke().ConfigureAwait(false);
                return func(value);
            });

        public TaskIO<TResult> Select<TResult>(Func<T, TResult> selector)
            => taskIO.Map(selector);

        public TaskIO<TResult> SelectMany<TResult>(Func<T, TaskIO<TResult>> binder)
            => taskIO.Bind(binder);

        public TaskIO<TResult> SelectMany<TIntermediate, TResult>(
            Func<T, TaskIO<TIntermediate>> binder,
            Func<T, TIntermediate, TResult> projector)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                var intermediate = await binder(value).Invoke().ConfigureAwait(false);
                return projector(value, intermediate);
            });

        public TaskIO<TResult> Then<TResult>(TaskIO<TResult> next)
            => TaskIO.From(async () =>
            {
                await taskIO.Invoke().ConfigureAwait(false);
                return await next.Invoke().ConfigureAwait(false);
            });

        public TaskIO<T> Delay(TimeSpan delay)
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                await Task.Delay(delay).ConfigureAwait(false);
                return value;
            });

        public TaskIO<T> WithCancellation(CancellationToken token)
            => new(taskIO.Invoke().WaitAsync(token));

        public TaskIO<TResult> Using<TResource, TResult>(
            Func<T, TaskIO<TResource>> resourceFactory,
            Func<T, TResource, TaskIO<TResult>> body)
            where TResource : IAsyncDisposable
            => TaskIO.From(async () =>
            {
                var value = await taskIO.Invoke().ConfigureAwait(false);
                await using var resource = await resourceFactory(value).Invoke().ConfigureAwait(false);
                return await body(value, resource).Invoke().ConfigureAwait(false);
            });

        public Option<T> ToOption()
        {
            var task = taskIO.Invoke();
            if (task.IsCompletedSuccessfully)
            {
                var result = task.Result;
                return result is null ? Option<T>.None : Option<T>.Some(result);
            }

            return Option<T>.None;
        }

        public TaskIO<T> ToTaskIO()
            => taskIO;
    }

    // Instance-style helpers for string manipulation.
    extension(string value)
    {
        /// <summary>
        /// Removes all whitespace characters from the string.
        /// </summary>
        public string WithoutWhitespace => new string(value.Where(static c => !char.IsWhiteSpace(c)).ToArray());

        /// <summary>
        /// Splits the string on whitespace boundaries into words.
        /// </summary>
        public string[] Words => value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    // Static-style string operators for terse transformations.
    extension(string)
    {
        /// <summary>
        /// Indicates whether the string is null or whitespace.
        /// </summary>
        public static bool operator !(string value)
            => string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Trims leading and trailing whitespace from the string.
        /// </summary>
        public static string operator ~(string value)
            => value.Trim();

        /// <summary>
        /// Repeats the string <paramref name="count"/> times.
        /// </summary>
        public static string operator *(string value, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return string.Concat(Enumerable.Repeat(value, count));
        }

        /// <summary>
        /// Repeats the string <paramref name="count"/> times (commutative overload).
        /// </summary>
        public static string operator *(int count, string value)
            => value * count;

        /// <summary>
        /// Splits the string using the supplied character separator.
        /// </summary>
        public static string[] operator /(string value, char separator)
            => value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Splits the string using the supplied string separator.
        /// </summary>
        public static string[] operator /(string value, string separator)
            => value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Replaces occurrences of <c>replacement.Old</c> with <c>replacement.New</c>.
        /// </summary>
        public static string operator /(string value, (string Old, string New) replacement)
            => value.Replace(replacement.Old, replacement.New);

        /// <summary>
        /// Removes all occurrences of <paramref name="fragment"/>.
        /// </summary>
        public static string operator -(string value, string fragment)
            => value.Replace(fragment, string.Empty);

        /// <summary>
        /// Returns the trailing <paramref name="length"/> characters.
        /// </summary>
        public static string operator %(string value, int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            return length >= value.Length ? value : value[^length..];
        }

        /// <summary>
        /// Returns the leading <paramref name="length"/> characters.
        /// </summary>
        public static string operator <<(string value, int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            return length >= value.Length ? value : value[..length];
        }

        /// <summary>
        /// Skips the first <paramref name="length"/> characters.
        /// </summary>
        public static string operator >>(string value, int length)
        {
            if (length <= 0)
            {
                return value;
            }

            return length >= value.Length ? string.Empty : value[length..];
        }

        /// <summary>
        /// Returns the distinct characters common to both strings.
        /// </summary>
        public static string operator &(string left, string right)
            => new string(left.Where(right.Contains).Distinct().ToArray());

        /// <summary>
        /// Returns the characters unique to either string (symmetric difference).
        /// </summary>
        public static string operator ^(string left, string right)
            => new string(
                left.Where(c => !right.Contains(c))
                    .Concat(right.Where(c => !left.Contains(c)))
                    .ToArray());

        /// <summary>
        /// Applies a projection to the string using pipeline syntax.
        /// </summary>
        public static string operator |(string value, Func<string, string> projector)
            => projector(value);
    }
}
