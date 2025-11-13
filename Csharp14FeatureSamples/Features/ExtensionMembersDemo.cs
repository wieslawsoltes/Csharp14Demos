using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FunctionalExtensions;
using static System.Console;
using static FunctionalExtensions.SequenceOperators;

namespace Csharp14FeatureSamples.Features;

public sealed class ExtensionMembersDemo : IFeatureDemo
{
    public string Title => "Extension members";

    public void Run()
    {
        int[] numbers = [1, 2, 3, 4];
        string[] words =
        [
            "apple",
            "apricot",
            "banana",
            "blueberry",
            "cherry",
            "clementine",
            "date",
        ];

        ShowInstanceExtensionMembers(numbers);
        ShowOperatorPoweredPipelines(numbers, words);
        ShowOperatorAlgebra(numbers);
        ShowSetStyleComposition();
        ShowJoinsAndRelationalOperators();
        ShowHigherOrderSequenceAnalysis(numbers);
        ShowTerseOperatorPlayground(numbers, words);
        ShowOptionMonadPlayground(numbers);
        ShowTryMonadPlayground();
        ShowResultMonadPlayground(numbers);
        ShowIOMonadPlayground();
        ShowReaderMonadPlayground();
        ShowWriterMonadPlayground();
        ShowValidationApplicativePlayground();
        ShowTaskMonadPlayground();
        ShowTaskResultMonadPlayground();
        ShowReaderTaskResultPlayground();
        ShowStateTaskResultPlayground();
        ShowStateMonadPlayground();
        ShowContinuationMonadPlayground();
        ShowMonadConversionPlayground();
        ShowStringOperatorPlayground();
    }

    private static void ShowInstanceExtensionMembers(int[] numbers)
    {
        _ = $"numbers.IsEmpty => {numbers.IsEmpty}" >> WriteLine;

        var odds = numbers.Filter(n => n % 2 == 1).ToList();
        _ = $"numbers.Filter(n => n % 2 == 1) => [{string.Join(", ", odds)}]" >> WriteLine;

        var explicitIdentity = IEnumerable<int>.Identity;
        _ = $"IEnumerable<int>.Identity.Any() => {explicitIdentity.Any()}" >> WriteLine;

        var combined = numbers | [5, 6];
        _ = $"numbers | new[] {{ 5, 6 }} => [{string.Join(", ", combined)}]" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowOperatorPoweredPipelines(int[] numbers, IReadOnlyList<string> words)
    {
        _ = "Operator-powered pipelines:" >> WriteLine;

        var evenSquaresPipeline = Filter<int>(static n => n % 2 == 0)
            .Then(Map<int, int>(static n => n * n))
            .Then(OrderByDescending<int, int>(static n => n));

        var evenSquares =
            numbers
            | evenSquaresPipeline
            | Take<int>(3)
            | ToList<int>();

        _ = $"numbers |> Filter |> Map |> OrderByDescending |> Take 3 => [{evenSquares | ", "}]" >> WriteLine;

        _ = (
            numbers
            | Filter<int>(static n => n % 2 == 0)
            | Map<int, int>(static n => n * n)
            | Sum<int>()
        ) >> (total => { _ = $"Sum of even squares => {total}" >> WriteLine; });

        _ = (
            numbers
            | Map<int, double>(static n => double.CreateChecked(n))
            | Average<double>()
        ) >> (avg => { _ = $"Average of numbers => {avg}" >> WriteLine; });

        int[][] nested =
        [
            [1, 2, 3],
            [2, 3, 4],
            [4, 5, 6],
        ];

        _ = (
            nested
            | Bind<int[], int>(inner => inner)
            | Distinct<int>()
            | OrderBy<int, int>(static n => n)
            | ToArray<int>()
        ) >> (array => { _ = $"Flattened distinct sequence => [{array | ", "}]" >> WriteLine; });

        var groupedWords =
            words
            | GroupBy<string, char>(static word => word[0])
            | OrderBy<IGrouping<char, string>, char>(static grouping => grouping.Key)
            | Map<IGrouping<char, string>, string>(grouping => $"{grouping.Key}: {string.Join(", ", grouping)}")
            | ToList<string>();

        _ = "Grouped words by initial:" >> WriteLine;
        foreach (var line in groupedWords)
        {
            _ = $"  {line}" >> WriteLine;
        }

        var allUnderTen =
            numbers
            | All<int>(static n => n < 10);

        var anyGreaterThanThree =
            numbers
            | Any<int>(static n => n > 3);

        _ = $"All numbers < 10 => {allUnderTen}" >> WriteLine;
        _ = $"Any number > 3 => {anyGreaterThanThree}" >> WriteLine;

        var factorialOfFive =
            Enumerable.Range(1, 5)
            | Aggregate<int, int>(1, static (acc, value) => acc * value);

        _ = $"Factorial of 5 => {factorialOfFive}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowOperatorAlgebra(int[] numbers)
    {
        _ = "Operator algebra on pipelines:" >> WriteLine;

        var doublePipeline = Map<int, int>(static n => n * 2);
        var incrementPipeline = Map<int, int>(static n => n + 1);
        var doubleThenIncrement = doublePipeline.Then(incrementPipeline);

        var composedResult =
            numbers
            | doubleThenIncrement
            | ToList<int>();

        _ = $"numbers |> (Map(*2) >> Map(+1)) => [{composedResult | ", "}]" >> WriteLine;

        var evenFilter = Filter<int>(static n => n % 2 == 0);
        var multipleOfThreeFilter = Filter<int>(static n => n % 3 == 0);

        var unionFilter = evenFilter + multipleOfThreeFilter;
        var intersectFilter = evenFilter & multipleOfThreeFilter;
        var symmetricFilter = evenFilter ^ multipleOfThreeFilter;

        _ = (
            numbers
            | unionFilter
            | ToList<int>()
        ) >> (result => { _ = $"(Filter even + Filter mul-of-3) => [{result | ", "}]" >> WriteLine; });

        _ = (
            numbers
            | intersectFilter
            | ToList<int>()
        ) >> (result => { _ = $"(Filter even & Filter mul-of-3) => [{result | ", "}]" >> WriteLine; });

        _ = (
            numbers
            | symmetricFilter
            | ToList<int>()
        ) >> (result => { _ = $"(Filter even ^ Filter mul-of-3) => [{result | ", "}]" >> WriteLine; });

        _ = (
            numbers
            | ~Map<int, int>(static n => n * n)
            | ToList<int>()
        ) >> (result => { _ = $"numbers |> ~Map(square) => [{result | ", "}]" >> WriteLine; });

        _ = (
            numbers
            | (Take<int>(2) * 3)
            | ToList<int>()
        ) >> (result => { _ = $"numbers |> (Take 2 * 3) => [{result | ", "}]" >> WriteLine; });

        _ = string.Empty >> WriteLine;
    }

    private static void ShowSetStyleComposition()
    {
        _ = "Set-style composition:" >> WriteLine;

        int[] left = [1, 2, 3, 4, 5, 6];
        int[] right = [4, 5, 6, 7, 8];

        var concatenated = !(left | right);
        var unionSet = !(left + right);
        var intersectSet = !(left & right);
        var exceptSet = !(left - right);
        var symmetricSet = !(left ^ right);
        var appended = !(left + 42);
        var prepended = !(0 + right);
        var reversedLeft = !~left;
        var repeatedTwice = !(left * 2);

        _ = $"left | right => [{concatenated | ", "}]" >> WriteLine;
        _ = $"left + right (Union) => [{unionSet | ", "}]" >> WriteLine;
        _ = $"left & right (Intersect) => [{intersectSet | ", "}]" >> WriteLine;
        _ = $"left - right (Except) => [{exceptSet | ", "}]" >> WriteLine;
        _ = $"left ^ right (Symmetric diff) => [{symmetricSet | ", "}]" >> WriteLine;
        _ = $"left + 42 => [{appended | ", "}]" >> WriteLine;
        _ = $"0 + right => [{prepended | ", "}]" >> WriteLine;
        _ = $"~left => [{reversedLeft | ", "}]" >> WriteLine;
        _ = $"left * 2 => [{repeatedTwice | ", "}]" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowJoinsAndRelationalOperators()
    {
        _ = "Joins and relational operators:" >> WriteLine;

        Author[] authors =
        [
            new(1, "Alice"),
            new(2, "Bob"),
            new(3, "Catherine"),
        ];

        Book[] books =
        [
            new(1, "Functional C#"),
            new(1, "Category Theory Notes"),
            new(2, "LINQ Cookbook"),
        ];

        var authoredBooks =
            authors
            | Join<Author, Book, int, string>(
                books,
                author => author.Id,
                book => book.AuthorId,
                (author, book) => $"{author.Name} → {book.Title}")
            | ToList<string>();

        _ = "Inner join (authors ↔ books):" >> WriteLine;
        foreach (var line in authoredBooks)
        {
            _ = $"  {line}" >> WriteLine;
        }

        var authorsWithOptionalBooks = (authors
            | LeftJoin<Author, Book, int>(
                books,
                author => author.Id,
                book => book.AuthorId)).ToList();

        _ = "Left join (authors with optional books):" >> WriteLine;
        foreach (var pair in authorsWithOptionalBooks)
        {
            var titles = pair.Matches.Select(book => book.Title).DefaultIfEmpty("<none>");
            _ = $"  {pair.Item.Name}: {string.Join(", ", titles)}" >> WriteLine;
        }

        _ = string.Empty >> WriteLine;
    }

    private static void ShowHigherOrderSequenceAnalysis(int[] numbers)
    {
        _ = "Higher-order sequence analysis:" >> WriteLine;

        var runningTotals =
            numbers
            | Scan<int, int>(0, static (acc, value) => acc + value)
            | ToList<int>();

        _ = $"Running totals => [{string.Join(", ", runningTotals)}]" >> WriteLine;

        var pairwiseTransitions =
            numbers
            | Pairwise<int>()
            | Map<(int Previous, int Current), string>(pair => $"{pair.Previous}->{pair.Current}")
            | ToList<string>();

        _ = $"Pairwise transitions => [{string.Join(", ", pairwiseTransitions)}]" >> WriteLine;

        var slidingWindows =
            numbers
            | Window<int>(size: 3, allowPartial: true)
            | Map<IReadOnlyList<int>, string>(window => $"[{string.Join(", ", window)}]")
            | ToList<string>();

        _ = "Sliding windows (size 3, partial allowed):" >> WriteLine;
        foreach (var window in slidingWindows)
        {
            _ = $"  {window}" >> WriteLine;
        }

        (int sum, int count) = numbers | (Sum<int>() & Count<int>());
        var sumSquared = numbers | Sum<int>().Select(static total => total * total);

        _ = $"Sum => {sum}, Count => {count}, Sum² => {sumSquared}" >> WriteLine;

        _ = (
            numbers
            | Contains(3)
        ) >> (result => { _ = $"numbers contains 3 => {result}" >> WriteLine; });

        _ = (
            numbers
            | SequenceEqual(new[] { 1, 2, 3, 4 })
        ) >> (result => { _ = $"numbers equals [1, 2, 3, 4] => {result}" >> WriteLine; });

        _ = string.Empty >> WriteLine;
    }

    private static void ShowTerseOperatorPlayground(int[] numbers, IReadOnlyList<string> words)
    {
        _ = "Terse operator playground:" >> WriteLine;

        var turboEvenSquares = numbers
            | Filter<int>(static n => n % 2 == 0)
            | Map<int, int>(static n => n * n)
            | Take<int>(3)
            | ", ";
        _ = $"numbers |> Filter |> Map |> Take => {turboEvenSquares}" >> WriteLine;

        var chunked = (numbers / 2).Select(chunk => chunk | ", ") | " / ";
        _ = $"numbers / 2 => {chunked}" >> WriteLine;

        var tailTwo = (numbers % 2) | ", ";
        _ = $"numbers % 2 => {tailTwo}" >> WriteLine;

        var firstThree = (numbers << 3) | ", ";
        _ = $"numbers << 3 => {firstThree}" >> WriteLine;

        var skipTwo = (numbers >> 2) | ", ";
        _ = $"numbers >> 2 => {skipTwo}" >> WriteLine;

        int[] moreNumbers = [5, 6, 7];
        var unionMinus = ((numbers + moreNumbers) - new[] { 1, 2 }) | ", ";
        _ = $"(numbers + moreNumbers) - [1, 2] => {unionMinus}" >> WriteLine;

        var prependedAppended = !(0 + numbers + 99);
        _ = $"0 + numbers + 99 => {prependedAppended | ", "}" >> WriteLine;

        var reversedArrow = ~numbers | " -> ";
        _ = $"~numbers => {reversedArrow}" >> WriteLine;

        var tripled = !(numbers * 3);
        _ = $"numbers * 3 => {tripled | ", "}" >> WriteLine;

        string[] wordsMore = ["cipher", "syntax", "lambda"];
        var alphaBlend = ((words + wordsMore) ^ new[] { "banana", "cipher" }) | " | ";
        _ = $"words XOR blend => {alphaBlend}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowOptionMonadPlayground(int[] numbers)
    {
        _ = "Haskell-inspired option playground:" >> WriteLine;

        static Option<int> ParseInt(string text)
            => int.TryParse(text, out var value) ? Option<int>.Some(value) : Option<int>.None;

        var parsedAndDoubled =
            Option.FromNullable("42")
            .Bind(ParseInt)
            .Map(static n => n * 2);
        _ = $"\"42\" |> parseInt |> (*2) => {parsedAndDoubled.Match(n => n.ToString(), () => "<none>")}" >> WriteLine;

        var guarded =
            Option.Some(21)
            .Where(static n => n % 2 == 0)
            .Match(_ => "Guard preserved the value", () => "Guard filtered the odd value");
        _ = guarded >> WriteLine;

        var applicative =
            Option.Some<Func<int, int>>(static x => x * 3)
            * Option.Some(14);
        _ = $"Some(x => x * 3) <*> Some(14) => {applicative.Match(n => n.ToString(), () => "<none>")}" >> WriteLine;

        var applicativePair =
            Option.Some<Func<int, Func<int, string>>>(static x => y => $"({x}, {y})")
            * Option.Some(7)
            * Option.Some(8);
        _ = $"Curried pairing => {applicativePair.Match(s => s, () => "<none>")}" >> WriteLine;

        var fallback =
            Option<int>.None
            | Option.Some(99);
        _ = $"None <|> Some(99) => {fallback.ValueOr(-1)}" >> WriteLine;

        var firstEvenSqrt =
            numbers
            .Filter(static n => n % 2 == 0)
            .FirstOption()
            .Bind(static n => Option.Some(Math.Sqrt(n)));
        _ = $"numbers |> Filter even |> head |> sqrt => {firstEvenSqrt.Match(n => n.ToString("0.###"), () => "<none>")}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowTryMonadPlayground()
    {
        _ = "Haskell-inspired try playground:" >> WriteLine;

        var parseSuccess = Try.Run(() => int.Parse("512"));
        _ = $"Try.Run(\"512\") => {parseSuccess.Match(n => n.ToString(), ex => ex?.Message ?? "<none>")}" >> WriteLine;

        var parseFailure =
            Try.Run(() => int.Parse("oops"))
            .Recover(_ => 0);
        _ = $"Try.Run(\"oops\") |> Recover => {parseFailure.Match(n => n.ToString(), ex => ex?.Message ?? "<none>")}" >> WriteLine;

        var combined =
            Try.Run(() => 10)
            .Bind(a => Try.Run(() => 5).Map(b => a + b));
        _ = $"Try 10 + Try 5 => {combined.Match(n => n.ToString(), ex => ex?.Message ?? "<none>")}" >> WriteLine;

        var fallback =
            Try.Run(() => int.Parse("boom"))
            | Try.Run(() => 42);
        _ = $"Failure | Success => {fallback.Match(n => n.ToString(), ex => ex?.Message ?? "<none>")}" >> WriteLine;

        var tryToResult = Try.Run(() => "value".ToUpperInvariant()).ToResult();
        _ = $"Try -> Result => {tryToResult.Match(ok => ok, err => err ?? "<unknown>")}" >> WriteLine;

        var tryToOption = Try.Run(() => int.Parse("1024")).ToOption();
        _ = $"Try -> Option => {tryToOption.Match(n => n.ToString(), () => "<none>")}" >> WriteLine;

        var tryToIO = Try.Run(() => DateTime.UtcNow).ToIO();
        _ = $"Try -> IO => {tryToIO.Run():O}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowResultMonadPlayground(int[] numbers)
    {
        _ = "Haskell-inspired result playground:" >> WriteLine;

        static Result<int> ParseIntResult(string text)
            => int.TryParse(text, out var value)
                ? Result.Ok(value)
                : Result.Fail<int>($"'{text}' is not an integer");

        var halved =
            Result.Ok("64")
                .Bind(ParseIntResult)
                .Map(static n => n / 2)
                .Match(ok => ok.ToString(), err => err ?? "<unknown>");
        _ = $"Result.Ok(\"64\") |> parse |> (/2) => {halved}" >> WriteLine;

        var recovered =
            Result.Ok("oops")
                .Bind(ParseIntResult)
                .Recover(_ => -1)
                .Match(ok => ok.ToString(), err => err ?? "<unknown>");
        _ = $"Result.Ok(\"oops\") |> parse |> recover => {recovered}" >> WriteLine;

        var preferFirst =
            Result.Fail<int>("boom")
            | Result.Ok(5);
        _ = $"Error(\"boom\") <|> Ok(5) => {preferFirst.Match(ok => ok.ToString(), err => err ?? "<unknown>")}" >> WriteLine;

        var applicativeSum =
            Result.Ok<Func<int, Func<int, int>>>(static a => b => a + b)
            * ParseIntResult("10")
            * ParseIntResult("32");
        _ = $"Applicative sum => {applicativeSum.Match(ok => ok.ToString(), err => err ?? "<unknown>")}" >> WriteLine;

        var failureChain =
            ParseIntResult("not-a-number")
            .Bind(static n => Result.Ok(n * 2))
            .Recover(err => -1)
            .Match(ok => ok.ToString(), err => err ?? "<unknown>");
        _ = $"Failure chain with recover => {failureChain}" >> WriteLine;

        var optionToResult =
            numbers
                .FirstOption()
                .Match(
                    whenSome: Result.Ok<int>,
                    whenNone: () => Result.Fail<int>("Sequence empty"));
        _ = $"numbers.FirstOption() => {optionToResult.Match(ok => $"Ok({ok})", err => err ?? "<unknown>")}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowIOMonadPlayground()
    {
        _ = "Haskell-inspired IO playground:" >> WriteLine;

        var random = new Random(0);
        IO<int> nextRandom = IO.From(() => random.Next(1, 100));

        var randomPair =
            nextRandom.Bind(first =>
                nextRandom.Map(second => (first, second, Sum: first + second)));

        var firstRun = randomPair.Run();
        _ = $"Random pair #1 => {firstRun.first} + {firstRun.second} = {firstRun.Sum}" >> WriteLine;

        var secondRun = randomPair.Run();
        _ = $"Random pair #2 => {secondRun.first} + {secondRun.second} = {secondRun.Sum}" >> WriteLine;

        var timeStamp =
            IO.From(() => DateTimeOffset.UtcNow)
            .Map(time => time.ToString("HH:mm:ss.fff"));
        _ = $"Timestamp (lazy) => {timeStamp.Run()}" >> WriteLine;
        _ = $"Timestamp (second run) => {timeStamp.Run()}" >> WriteLine;

        var effectful =
            IO.Return("payload")
            .Tap(message => _ = $"  tap saw '{message}'" >> WriteLine)
            .Map(static message => message.ToUpperInvariant());
        _ = $"Effectful run => {effectful.Run()}" >> WriteLine;

        var applicativeGreeting =
            IO.Return<Func<string, Func<string, string>>>(static prefix => name => $"{prefix}, {name}!")
            * IO.Return("Hello")
            * IO.Return(Environment.UserName);
        _ = applicativeGreeting.Run() >> WriteLine;

        var divisor = 0;
        var ioResult =
            IO.From(() => Result.Try(() => 10 / divisor))
            .Map(result => result.Match(ok => $"Computation => {ok}", err => $"Recovered => {err}"));
        _ = $"IO<Result<int>> => {ioResult.Run()}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowReaderMonadPlayground()
    {
        _ = "Haskell-inspired reader playground:" >> WriteLine;

        var greetingReader =
            Reader.Ask<AppSettings>()
                .Map(settings => $"Hello from {settings.Environment} ({settings.Region})");

        var invoiceReader =
            Reader.Ask<AppSettings>()
                .Map(settings =>
                {
                    const decimal baseAmount = 100m;
                    var tax = baseAmount * (decimal)settings.TaxRate;
                    var total = baseAmount + tax;
                    return $"{settings.CurrencySymbol}{total:0.00} (tax {settings.CurrencySymbol}{tax:0.00})";
                });

        var euSettings = new AppSettings("Production", "EU", 0.21, "€");
        var usSettings = euSettings with { Region = "US", TaxRate = 0.07, CurrencySymbol = "$" };

        _ = $"Greeting (EU) => {greetingReader.Run(euSettings)}" >> WriteLine;
        _ = $"Greeting (US) => {greetingReader.Run(usSettings)}" >> WriteLine;

        _ = $"Invoice (EU) => {invoiceReader.Run(euSettings)}" >> WriteLine;
        _ = $"Invoice (US) => {invoiceReader.Run(usSettings)}" >> WriteLine;

        var localizedInvoice = invoiceReader.Local(settings => settings with { Region = "UK", TaxRate = 0.10, CurrencySymbol = "£" });
        _ = $"Invoice (Local UK) => {localizedInvoice.Run(euSettings)}" >> WriteLine;

        var readerFunc = greetingReader.ToFunc();
        _ = $"Reader.ToFunc()(QA) => {readerFunc(new AppSettings("QA", "CA", 0.15, "C$"))}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowWriterMonadPlayground()
    {
        _ = "Haskell-inspired writer playground:" >> WriteLine;

        var pipeline =
            Writer.Return<int, string>(5)
                .Bind(n => Writer.From(n * 2, $"Doubled => {n * 2}"))
                .Bind(n => Writer.From(n - 3, $"Subtracted 3 => {n}"))
                .AppendLog("Finalised");

        _ = $"Writer pipeline => {pipeline.PrettyPrint()}" >> WriteLine;

        _ = "Writer logs streamed via ToIO:" >> WriteLine;
        pipeline.ToIO(log => _ = $"  log: {log}" >> WriteLine).Run();

        var told =
            Writer.Tell<string>("Start job")
                .Bind(_ => Writer.From("Done", "Finish job"));
        _ = $"Writer.Tell => {told.PrettyPrint()}" >> WriteLine;

        var applicativeWriter =
            Writer.Return<Func<int, Func<int, int>>, string>(x => y => x * y)
            * Writer.Return<int, string>(6)
            * Writer.Return<int, string>(7);
        _ = $"Applicative Writer => {applicativeWriter.PrettyPrint()}" >> WriteLine;

        var valueOption = pipeline.Value.ToOption(static v => v > 0);
        _ = $"Writer.Value.ToOption => {valueOption.Match(v => v.ToString(), () => "<none>")}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowValidationApplicativePlayground()
    {
        _ = "Validation applicative playground:" >> WriteLine;

        var nameValidation = Validation.Success("Ada")
            .Ensure(static name => !string.IsNullOrWhiteSpace(name), "Name is required")
            .Ensure(static name => name.Length >= 3, "Name must have at least three characters");

        var emailValidation = Validation.Success("ada@example.com")
            .Ensure(static email => email.Contains('@'), "Email must contain '@'")
            .Ensure(static email => email.Contains('.'), "Email must contain '.'");

        var profileValidation =
            Validation.Success<Func<string, Func<string, string>>>(static name => email => $"{name} <{email}>")
            * nameValidation
            * emailValidation;

        _ = $"Profile validation => {profileValidation}" >> WriteLine;

        var invalidProfile =
            Validation.Success("Bo")
                .Ensure(static name => name.Length >= 3, "Name too short")
                .Ensure(static name => name.All(char.IsLetter), "Name must be alphabetic")
                .Combine(
                    Validation.Failure<string>("City is required"),
                    static (name, _) => name);

        _ = $"Invalid validation => [{string.Join(", ", invalidProfile.Errors)}]" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowTaskMonadPlayground()
    {
        _ = "Task/async monad playground:" >> WriteLine;

        var fetchUser =
            TaskIO.From(async () =>
            {
                await Task.Delay(20).ConfigureAwait(false);
                return "AsyncUser";
            });

        var fetchOrderCount =
            TaskIO.From(async () =>
            {
                await Task.Delay(15).ConfigureAwait(false);
                return 3;
            });

        var combined =
            fetchUser.Bind(user =>
                fetchOrderCount.Map(count => $"{user} has {count} open orders"));

        _ = combined.RunAsync().GetAwaiter().GetResult() >> WriteLine;

        var tapped =
            fetchOrderCount
                .Tap(async count =>
                {
                    _ = $"  observed count {count}" >> WriteLine;
                    await Task.CompletedTask;
                })
                .Map(static count => count * 2);

        _ = $"Tapped double => {tapped.RunAsync().GetAwaiter().GetResult()}" >> WriteLine;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(25);

        var slowOperation =
            TaskIO.From(async () =>
            {
                await Task.Delay(100, cts.Token).ConfigureAwait(false);
                return "Slow result";
            });

        try
        {
            slowOperation.WithCancellation(cts.Token).RunAsync().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            _ = "Cancellation triggered" >> WriteLine;
        }

        var usingDemo =
            fetchUser.Using(
                user => TaskIO.Return(new AsyncDisposableProbe($"resource for {user}")),
                (user, resource) => TaskIO.Return($"{user} used {resource.Label}"));

        _ = usingDemo.RunAsync().GetAwaiter().GetResult() >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowTaskResultMonadPlayground()
    {
        _ = "Task<Result> transformer playground:" >> WriteLine;

        var fetchUser =
            TaskResults.From(async () =>
            {
                await Task.Delay(20).ConfigureAwait(false);
                return Result<string>.Ok("AsyncUser");
            });

        var fetchDiscount =
            TaskResults.From(async () =>
            {
                await Task.Delay(15).ConfigureAwait(false);
                return Result<decimal>.Ok(0.15m);
            });

        var summary =
            fetchUser.SelectMany(user =>
                fetchDiscount.Select(discount => $"{user} discount => {discount:P0}"));

        var summaryResult = summary.RunAsync().GetAwaiter().GetResult();
        _ = $"TaskResult.SelectMany => {summaryResult.Match(value => value, error => error ?? "<unknown>")}" >> WriteLine;

        var fallback =
            TaskResults.Fail<int>("Service unavailable")
                .OrElse(() => TaskResults.Return(5))
                .RunAsync().GetAwaiter().GetResult();
        _ = $"TaskResult fallback => {fallback.Match(value => value.ToString(), error => error ?? "<unknown>")}" >> WriteLine;

        var tapped =
            fetchUser.Tap(static user =>
            {
                _ = $"  audit {user}" >> WriteLine;
            });
        _ = $"TaskResult tap => {tapped.RunAsync().GetAwaiter().GetResult().Match(value => value, error => error ?? "<unknown>")}" >> WriteLine;

        var option = summary.ToOptionAsync().GetAwaiter().GetResult();
        _ = $"TaskResult -> Option => {option.Match(value => value, () => "<none>")}" >> WriteLine;

        var asTaskIO = summary.ToTaskIO(error => new InvalidOperationException(error ?? "unknown"));
        _ = $"TaskResult -> TaskIO => {asTaskIO.RunAsync().GetAwaiter().GetResult()}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowReaderTaskResultPlayground()
    {
        _ = "Reader<TaskResult> transformer playground:" >> WriteLine;

        var environmentGreeting =
            ReaderTaskResults.Ask<AppSettings>()
                .Map(settings => $"Hello from {settings.Environment} ({settings.Region})");

        var loadDiscount =
            ReaderTaskResults.From<AppSettings, double>(settings =>
                settings.TaxRate >= 0
                    ? TaskResults.Return(settings.TaxRate)
                    : TaskResults.Fail<double>("Tax rate invalid"));

        var combined =
            environmentGreeting.SelectMany(greeting =>
                loadDiscount.Select(discount => $"{greeting} | tax {discount:P0}"));

        var euSettings = new AppSettings("Production", "EU", 0.21, "€");
        var usSettings = euSettings with { Region = "US", TaxRate = 0.07, CurrencySymbol = "$" };

        var euResult = combined.RunAsync(euSettings).GetAwaiter().GetResult();
        _ = $"ReaderTaskResult (EU) => {euResult.Match(value => value, error => error ?? "<unknown>")}" >> WriteLine;

        var usResult = combined.Local(settings => settings with { TaxRate = 0.05 }).RunAsync(usSettings).GetAwaiter().GetResult();
        _ = $"ReaderTaskResult (US local override) => {usResult.Match(value => value, error => error ?? "<unknown>")}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowStateTaskResultPlayground()
    {
        _ = "State<TaskResult> transformer playground:" >> WriteLine;

        var workflow =
            StateTaskResults.Get<int>()
                .Bind(previous => StateTaskResults.Modify<int>(static s => s + 10)
                    .Select(_ => previous))
                .Bind(previous => StateTaskResults.Modify<int>(static s => s * 2)
                    .Select(_ => $"Started at {previous}, now doubled after increment"));

        var runResult = workflow.ToTaskResult(5).RunAsync().GetAwaiter().GetResult();
        _ = $"Run => {runResult.Match(result => $"{result.Value} | state {result.State}", error => error ?? "<unknown>")}" >> WriteLine;

        var evaluated = workflow.Evaluate(3).RunAsync().GetAwaiter().GetResult();
        _ = $"Evaluate => {evaluated.Match(value => value, error => error ?? "<unknown>")}" >> WriteLine;

        var executed = workflow.Execute(3).RunAsync().GetAwaiter().GetResult();
        _ = $"Execute => {executed.Match(value => value.ToString(), error => error ?? "<unknown>")}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowStateMonadPlayground()
    {
        _ = "Haskell-inspired state playground:" >> WriteLine;

        var counterWorkflow =
            State.Return<int, Unit>(Unit.Value)
                .Bind(_ => State.Modify<int>(static s => s + 1))
                .Bind(_ => State.Modify<int>(static s => s * 3))
                .Bind(_ => State.Get<int>())
                .Map(static total => $"Counter => {total}");

        var (summary, finalState) = counterWorkflow.RunState(2);
        _ = $"RunState(2) => {summary}, final state {finalState}" >> WriteLine;

        var evaluated = counterWorkflow.Evaluate(5);
        _ = $"Evaluate(5) => {evaluated}" >> WriteLine;

        var executed = counterWorkflow.Execute(7);
        _ = $"Execute(7) => {executed}" >> WriteLine;

        var ioState = counterWorkflow.ToIO(3);
        _ = $"State.ToIO(3) => {ioState.Run()}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowContinuationMonadPlayground()
    {
        _ = "Haskell-inspired continuation playground:" >> WriteLine;

        var pipeline =
            Continuation.Return<string, int>(5)
                .Map(static n => n * 2)
                .Bind(static n => Continuation.Return<string, int>(n + 10));

        var pipelineResult = pipeline.Run(result => $"Continuation produced {result}");
        _ = pipelineResult >> WriteLine;

        var earlyExit =
            Continuation.CallCC<string, int>(exit =>
                Continuation.Return<string, int>(1)
                    .Bind(_ => exit(99))
                    .Bind(_ => Continuation.Return<string, int>(0)));

        var earlyResult = earlyExit.Run(result => $"call/cc returned {result}");
        _ = earlyResult >> WriteLine;

        var contToIO = pipeline.ToIO(result => $"IO via continuation => {result}");
        _ = contToIO.Run() >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowMonadConversionPlayground()
    {
        _ = "Monad conversion playground:" >> WriteLine;

        string? nullableName = null;
        var nameOption = nullableName.ToOption();
        _ = $"null.ToOption() => {nameOption.Match(value => value, () => "<none>")}" >> WriteLine;

        var nameResult = nameOption
            .ToResult("Name missing")
            .Recover(_ => "Anonymous");
        _ = $"Option -> Result -> Recover => {nameResult.Match(value => value, err => err ?? "<unknown>")}" >> WriteLine;

        var numberResult = new Func<int>(() => 21 * 2).ToResult();
        _ = $"Func<int>.ToResult() => {numberResult.Match(n => n.ToString(), err => err ?? "<unknown>")}" >> WriteLine;

        var failingResult = new Func<int>(() => throw new InvalidOperationException("boom")).ToResult();
        _ = $"Failing func ToResult() => {failingResult.Match(n => n.ToString(), err => err ?? "<unknown>")}" >> WriteLine;

        var author = new Author(7, "Applicative Al");
        var authorIO = author.ToIO().Map(a => a.Name);
        _ = $"Author.ToIO().Map(name) => {authorIO.Run()}" >> WriteLine;

        var actionIO = new Action(() => _ = "  side-effect from Action.ToIO()" >> WriteLine).ToIO();
        _ = "Action.ToIO() run =>" >> WriteLine;
        actionIO.Run();
        _ = "  action completed" >> WriteLine;

        var ioOption =
            IO.From(() => "monads")
            .ToResult()
            .ToOption();
        _ = $"IO -> Result -> Option => {ioOption.Match(s => s, () => "<none>")}" >> WriteLine;

        var validationOption =
            Validation.Success(42)
                .Ensure(static value => value > 0, "Value must be positive")
                .ToOption();
        _ = $"Validation -> Option => {validationOption.Match(v => v.ToString(), () => "<none>")}" >> WriteLine;

        var taskOption = TaskIO.Return("completed").ToOption();
        _ = $"TaskIO -> Option => {taskOption.Match(v => v, () => "<none>")}" >> WriteLine;

        _ = string.Empty >> WriteLine;
    }

    private static void ShowStringOperatorPlayground()
    {
        _ = "String operator playground:" >> WriteLine;

        var raw = "  pipe-all-the-things  ";
        _ = $"!raw => {!raw}" >> WriteLine;

        var trimmed = ~raw;
        _ = $"~raw => '{trimmed}'" >> WriteLine;

        var shout = trimmed | (static s => s.ToUpperInvariant());
        _ = $"trimmed | upper => {shout}" >> WriteLine;

        var tokens = trimmed / '-';
        _ = $"trimmed / '-' => {tokens | ", "}" >> WriteLine;

        var spaced = trimmed / ("-", " ");
        _ = $"trimmed / (\"-\", \" \") => {spaced}" >> WriteLine;

        var noPipe = trimmed - "pipe";
        _ = $"trimmed - \"pipe\" => {noPipe}" >> WriteLine;

        _ = $"trimmed % 5 => {trimmed % 5}" >> WriteLine;
        _ = $"trimmed << 4 => {trimmed << 4}" >> WriteLine;
        _ = $"trimmed >> 5 => {trimmed >> 5}" >> WriteLine;

        _ = $"trimmed & \"aeiou\" => {trimmed & "aeiou"}" >> WriteLine;
        _ = $"trimmed ^ \"aeiou\" => {trimmed ^ "aeiou"}" >> WriteLine;

        _ = $"\"ha\" * 3 => {"ha" * 3}" >> WriteLine;
        _ = $"3 * \"ha\" => {3 * "ha"}" >> WriteLine;

        var zigZag = trimmed.WithoutWhitespace;
        _ = $"trimmed.WithoutWhitespace => {zigZag}" >> WriteLine;
    }

    private sealed record Author(int Id, string Name);

    private sealed record Book(int AuthorId, string Title);

    private sealed class AsyncDisposableProbe : IAsyncDisposable
    {
        public AsyncDisposableProbe(string label) => Label = label;

        public string Label { get; }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            _ = $"  disposing {Label}" >> WriteLine;
        }

        public override string ToString() => Label;
    }

    private sealed record AppSettings(string Environment, string Region, double TaxRate, string CurrencySymbol);
}
