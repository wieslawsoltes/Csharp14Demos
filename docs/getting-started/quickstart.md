# Quickstart: Minimal API + FunctionalExtensions

Build an ASP.NET Core minimal API endpoint that uses FunctionalExtensions to locate customers via `Option`, validate requests with the validation DSL, and return typed results. All code samples in this guide compile inside `docs/snippets/Quickstart/MinimalApiQuickstart.cs`; keep that file open so you can copy sections directly into your project.

## 1. Project Setup
```bash
dotnet new web -n QuoteApi
cd QuoteApi
dotnet add package FunctionalExtensions.TypeClasses --version 0.1.0-preview
```

Enable the preview language features in `QuoteApi.csproj`:
```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>14.0</LangVersion>
  <EnablePreviewFeatures>true</EnablePreviewFeatures>
</PropertyGroup>
```

## 2. Bootstrap the Host
Create `MinimalApiQuickstart.cs` in your API project and add the bootstrapping method. It registers the in-memory gateway and wires up the endpoint mapping extension.

```csharp
using FunctionalExtensions.Snippets.Quickstart;

var app = MinimalApiQuickstart.ConfigureQuickstartApp(args);
app.Run();
```

`ConfigureQuickstartApp` (from `docs/snippets/Quickstart/MinimalApiQuickstart.cs`) looks like this:

```csharp
public static WebApplication ConfigureQuickstartApp(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<ICustomerGateway>(_ =>
    {
        var gateway = new InMemoryCustomerGateway();
        gateway.Seed(new Customer(Guid.Parse("4a7e2e9d-ac27-4a6a-8f08-4b43fa1c2d74"), "Adventure Works", 50000m));
        gateway.Seed(new Customer(Guid.Parse("a2369f8d-31aa-4f1e-9c2b-c249bca7ff8c"), "Contoso Retail", 25000m));
        return gateway;
    });

    var app = builder.Build();
    app.MapQuoteEndpoints();
    return app;
}
```

## 3. Map an Option/Result Endpoint
Add the endpoint extension that combines the repository `Option<Customer>` with validation and returns an HTTP-friendly `Result`.

```csharp
public static void MapQuoteEndpoints(this WebApplication app)
{
    var validator = QuoteRequestValidator.Create();

    app.MapPost(
        "/quotes/{customerId:guid}",
        (Guid customerId, QuoteRequest request, ICustomerGateway customers) =>
        {
            var quoteResult = customers
                .Find(customerId)
                .ToResult("Customer was not found.")
                .Bind(customer => validator
                    .Apply(request)
                    .ToResult("Request failed validation.")
                    .Map(validRequest => QuoteEngine.Price(customer, validRequest)));

            return quoteResult.AsHttpResult();
        });
}
```

The pipeline flows as follows:
1. `ICustomerGateway.Find` returns `Option<Customer>`.
2. `ToResult` promotes it to `Result<Customer>` with a helpful error.
3. `Bind` sequences validation; `Map` projects a `QuoteResponse`.
4. `AsHttpResult` converts the final `Result` into `IResult`.

## 4. Define Domain Models & Validation
Keep the core request/response types next to the endpoint for clarity.

```csharp
public sealed record QuoteRequest(string ProductCode, decimal Amount, string Currency);
public sealed record QuoteResponse(decimal Amount, string Currency);
public sealed record Customer(Guid Id, string Name, decimal CreditLimit);
```

Author validation rules with the DSL so they are reusable outside of HTTP.

```csharp
public static class QuoteRequestValidator
{
    public static Validator<QuoteRequest> Create()
        => Validator<QuoteRequest>.Empty
            .Ensure(q => !string.IsNullOrWhiteSpace(q.ProductCode), "Product code is required.")
            .Ensure(q => q.Amount > 0, "Amount must be positive.")
            .Ensure(q => SupportedCurrencies.Contains(q.Currency), "Unsupported currency.");

    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD",
        "EUR",
        "GBP"
    };
}
```

## 5. Back the Endpoint with a Repository and Pricing Engine
Implement an `Option`-returning gateway plus a deterministic pricing rule.

```csharp
public interface ICustomerGateway
{
    Option<Customer> Find(Guid id);
}

public sealed class InMemoryCustomerGateway : ICustomerGateway
{
    private readonly Dictionary<Guid, Customer> _customers = new();

    public Option<Customer> Find(Guid id)
        => _customers.TryGetValue(id, out var customer)
            ? Option<Customer>.Some(customer)
            : Option<Customer>.None;

    public void Seed(Customer customer)
    {
        _customers[customer.Id] = customer;
    }
}

public static class QuoteEngine
{
    public static QuoteResponse Price(Customer customer, QuoteRequest request)
    {
        var approvedAmount = Math.Min(request.Amount, customer.CreditLimit);
        return new QuoteResponse(approvedAmount, request.Currency.ToUpperInvariant());
    }
}
```

## 6. Bridge Functional Types to HTTP
The helper extensions used in the endpoint live alongside the rest of the snippet and keep controllers clean.

```csharp
public static class QuickstartResultExtensions
{
    public static Result<T> ToResult<T>(this Option<T> option, string error)
        => option.HasValue ? Result<T>.Ok(option.Value!) : Result<T>.Fail(error);

    public static Result<T> ToResult<T>(this Validation<T> validation, string errorPrefix)
    {
        if (validation.IsValid && validation.Value is { } value)
        {
            return Result<T>.Ok(value);
        }

        var message = validation.Errors.Count > 0
            ? $"{errorPrefix} :: {string.Join("; ", validation.Errors)}"
            : errorPrefix;

        return Result<T>.Fail(message);
    }

    public static Result<TResult> Bind<T, TResult>(this Result<T> result, Func<T, Result<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return result.IsSuccess
            ? binder(result.Value!)
            : Result<TResult>.Fail(result.Error ?? "Operation failed.");
    }

    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return result.IsSuccess
            ? Result<TResult>.Ok(mapper(result.Value!))
            : Result<TResult>.Fail(result.Error ?? "Operation failed.");
    }

    public static IResult AsHttpResult<T>(this Result<T> result)
        => result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
}
```

## 7. Run the API
```bash
dotnet run
```

Create a valid quote:
```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -d '{"productCode":"subscription","amount":1200,"currency":"USD"}' \
  https://localhost:5001/quotes/4a7e2e9d-ac27-4a6a-8f08-4b43fa1c2d74
```

Submit an invalid payload to see accumulated errors returned via `Results.Problem`.

## 8. Keep Snippets Verified
Every example in this guide is compiled by `docs/snippets/FunctionalExtensions.Snippets.csproj`. Run the snippet tests anytime you update documentation:
```bash
dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj
```

Failing builds mean the docs or snippets drifted—update both together so the quickstart stays trustworthy.
