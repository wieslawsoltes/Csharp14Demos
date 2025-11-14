using System;
using System.Collections.Generic;
using FunctionalExtensions;
using FunctionalExtensions.ValidationDsl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionalExtensions.Snippets.Quickstart;

public static class MinimalApiQuickstart
{
    #region quickstart_minimal_api
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
    #endregion

    #region quickstart_config
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
    #endregion
}

#region quickstart_models
public sealed record QuoteRequest(string ProductCode, decimal Amount, string Currency);
public sealed record QuoteResponse(decimal Amount, string Currency);
public sealed record Customer(Guid Id, string Name, decimal CreditLimit);
#endregion

#region quickstart_validator
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
#endregion

#region quickstart_gateway
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
#endregion

#region quickstart_engine
public static class QuoteEngine
{
    public static QuoteResponse Price(Customer customer, QuoteRequest request)
    {
        var approvedAmount = Math.Min(request.Amount, customer.CreditLimit);
        return new QuoteResponse(approvedAmount, request.Currency.ToUpperInvariant());
    }
}
#endregion

#region quickstart_result_extensions
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
#endregion
