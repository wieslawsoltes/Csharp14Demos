# How-To: Compose Async Pipelines with `TaskResult`

FunctionalExtensions pairs `TaskResult<T>` with effect helpers (`TaskIO`, `HttpClientTaskResults`, `TaskResultDoBuilder`) so you can build asynchronous workflows that still produce explicit success/failure information. This guide shows how to wrap HTTP calls, compose caching + domain logic, and expose the result through ASP.NET Core minimal APIs—without scattering `try/catch` blocks.

All snippets compile inside `docs/snippets/HowTo/AsyncPipelines.cs`.

## 1. Lift HTTP calls into `TaskResult`
Use the `HttpClientTaskResults` extensions to convert `HttpClient` operations into `TaskResult<T>` values that capture non-success status codes, JSON failures, and cancellations.

```csharp
public static TaskResult<CustomerProfile> FetchProfileAsync(HttpClient client, Guid id, CancellationToken cancellationToken)
    => client.GetJsonTaskResult<CustomerProfile>($"https://api.example.com/customers/{id}", cancellationToken);
```
_Snippet: `AsyncPipelines.cs#region async_fetch`_

Benefits:
- You get consistent error messages (`HTTP 404 Not Found: ...`).
- Cancellation tokens flow through `HttpClient` and surface as failed results instead of thrown exceptions.
- Downstream code can compose the result monadically.

## 2. Orchestrate multi-step workflows with `TaskResultDoBuilder`
`TaskResultDoBuilder` gives you a `do`-notation style scope where each awaited `TaskResult<T>` either returns a value or short-circuits with its failure. This keeps your async code linear and readable.

```csharp
public static TaskResult<CustomerInsights> LoadCustomerInsights(
    HttpClient client,
    ICreditService credit,
    IProfileCache cache,
    Guid id,
    CancellationToken cancellationToken)
{
    return Do.Run(async scope =>
    {
        if (cache.TryGet(id, out var cached))
        {
            var cachedScore = await scope.Bind(credit.FetchScoreAsync(id, cancellationToken)).ConfigureAwait(false);
            return new CustomerInsights(cached, cachedScore);
        }

        var profile = await scope.Bind(FetchProfileAsync(client, id, cancellationToken)).ConfigureAwait(false);
        scope.Ensure(string.Equals(profile.Status, "active", StringComparison.OrdinalIgnoreCase), "Customer is not active.");

        var score = await scope.Bind(credit.FetchScoreAsync(id, cancellationToken)).ConfigureAwait(false);
        cache.Store(profile);
        return new CustomerInsights(profile, score);
    });
}
```
_Snippet: `AsyncPipelines.cs#region async_pipeline`_

What’s happening:
1. Cache hit returns immediately but still fetches a fresh credit score.
2. Cache miss fetches the profile via `TaskResult`, checks business rules with `Ensure`, then fetches the score.
3. Any failed HTTP call, validation, or exception throws `TaskResultShortCircuitException`, which the builder turns into a failed `TaskResult`.

## 3. Surface `TaskResult` in minimal APIs
Expose the composed workflow through ASP.NET Core without rethrowing exceptions. Convert the `TaskResult<T>` into an `IResult` that your API can return directly.

```csharp
public static void MapInsightEndpoint(this WebApplication app, HttpClient client, ICreditService credit, IProfileCache cache)
{
    app.MapGet("/customers/{id:guid}/insights", (Guid id, CancellationToken cancellationToken) =>
        ToHttpResult(LoadCustomerInsights(client, credit, cache, id, cancellationToken)));
}

public static async Task<IResult> ToHttpResult<T>(TaskResult<T> taskResult)
{
    var result = await taskResult.Invoke().ConfigureAwait(false);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
}
```
_Snippets: `AsyncPipelines.cs#region async_endpoint` and `#region async_http_result`_

Tips:
- Centralize the conversion so every endpoint shares consistent error semantics.
- Use `Result<T>.Match` if you need to map different error codes (e.g., 404 vs 500) based on the message.
- Combine with `ReaderTaskResult<HttpClient, T>` when you want to inject `HttpClient` via dependency injection instead of passing it manually.

## 4. Testing & Troubleshooting
- Run `dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj` to ensure your documentation snippets and real code stay in sync.
- When debugging failures, log the `Result<T>.Error` string before converting to HTTP; it already contains status codes and payload excerpts from the helper.
- If you need retries, wrap the `TaskResult` in a `TaskResults.From(async () => { ... })` loop or layer Polly around the `HttpClient`—the `Result` surface stays the same.
