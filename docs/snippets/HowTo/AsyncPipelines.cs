using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FunctionalExtensions;
using FunctionalExtensions.Computation;
using FunctionalExtensions.Effects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FunctionalExtensions.Snippets.HowTo;

public static class AsyncPipelines
{
    private static readonly TaskResultDoBuilder Do = new();

    #region async_fetch
    public static TaskResult<CustomerProfile> FetchProfileAsync(HttpClient client, Guid id, CancellationToken cancellationToken)
        => client.GetJsonTaskResult<CustomerProfile>($"https://api.example.com/customers/{id}", cancellationToken);
    #endregion

    #region async_pipeline
    public static TaskResult<CustomerInsights> LoadCustomerInsights(
        HttpClient client,
        ICreditService credit,
        IProfileCache cache,
        Guid id,
        CancellationToken cancellationToken)
    {
        Func<TaskResultDoScope, ValueTask<CustomerInsights>> workflow = async scope =>
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
        };

        return Do.Run(workflow);
    }
    #endregion

    #region async_endpoint
    public static void MapInsightEndpoint(this WebApplication app, HttpClient client, ICreditService credit, IProfileCache cache)
    {
        app.MapGet("/customers/{id:guid}/insights", (Guid id, CancellationToken cancellationToken) =>
            ToHttpResult(LoadCustomerInsights(client, credit, cache, id, cancellationToken)));
    }
    #endregion

    #region async_http_result
    public static async Task<IResult> ToHttpResult<T>(TaskResult<T> taskResult)
    {
        var result = await taskResult.Invoke().ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }
    #endregion
}

public interface IProfileCache
{
    bool TryGet(Guid id, out CustomerProfile profile);
    void Store(CustomerProfile profile);
}

public interface ICreditService
{
    TaskResult<decimal> FetchScoreAsync(Guid customerId, CancellationToken cancellationToken);
}

public sealed record CustomerProfile(Guid Id, string Name, string Status);
public sealed record CustomerInsights(CustomerProfile Profile, decimal CreditScore);
