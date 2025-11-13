using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using FunctionalExtensions;

namespace FunctionalExtensions.Effects;

/// <summary>
/// HTTP-centric helpers that wrap <see cref="HttpClient"/> operations in <see cref="TaskResult{T}"/>.
/// </summary>
public static class HttpClientTaskResults
{
    extension(HttpClient client)
    {
        /// <summary>
        /// Sends <paramref name="request"/> and converts the outcome into a <see cref="TaskResult{T}"/> that fails on non-success status codes.
        /// </summary>
        public TaskResult<HttpResponseMessage> SendTaskResult(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new TaskResult<HttpResponseMessage>(ExecuteAsync());

            async Task<Result<HttpResponseMessage>> ExecuteAsync()
            {
                try
                {
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return Result<HttpResponseMessage>.Ok(response);
                    }

                    var body = response.Content is null
                        ? null
                        : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    return Result<HttpResponseMessage>.Fail(BuildHttpError(response, body));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return Result<HttpResponseMessage>.Fail("HTTP request was cancelled.");
                }
                catch (Exception ex)
                {
                    return Result<HttpResponseMessage>.Fail(ex.Message);
                }
            }
        }

        /// <summary>
        /// Performs an HTTP GET and deserializes the JSON payload into <typeparamref name="TResponse"/>.
        /// </summary>
        public TaskResult<TResponse> GetJsonTaskResult<TResponse>(
            string requestUri,
            CancellationToken cancellationToken = default,
            JsonSerializerOptions? serializerOptions = null)
        {
            ArgumentNullException.ThrowIfNull(requestUri);

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return client.SendTaskResult(request, cancellationToken)
                .Bind(response => ReadJsonAsync<TResponse>(response, cancellationToken, serializerOptions));
        }

        /// <summary>
        /// Performs an HTTP POST with a JSON body and returns the deserialized JSON response.
        /// </summary>
        public TaskResult<TResponse> PostJsonTaskResult<TRequest, TResponse>(
            string requestUri,
            TRequest payload,
            CancellationToken cancellationToken = default,
            JsonSerializerOptions? serializerOptions = null)
        {
            ArgumentNullException.ThrowIfNull(requestUri);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(payload, options: serializerOptions)
            };

            return client.SendTaskResult(request, cancellationToken)
                .Bind(response => ReadJsonAsync<TResponse>(response, cancellationToken, serializerOptions));
        }

        /// <summary>
        /// Lifts an HTTP effect into a <see cref="ReaderTaskResult{TEnv, TValue}"/> using the client as the environment.
        /// </summary>
        public ReaderTaskResult<HttpClient, T> ToReaderTaskResult<T>(
            Func<HttpClient, TaskResult<T>> effect)
        {
            ArgumentNullException.ThrowIfNull(effect);
            return ReaderTaskResults.From<HttpClient, T>(effect);
        }
    }

    private static TaskResult<T> ReadJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        JsonSerializerOptions? serializerOptions)
    {
        return TaskResults.From(async () =>
        {
            using var responseScope = response;

            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(serializerOptions, cancellationToken).ConfigureAwait(false);
                if (result is null)
                {
                    return Result<T>.Fail("HTTP payload was empty.");
                }

                return Result<T>.Ok(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Result<T>.Fail("HTTP request was cancelled.");
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(ex.Message);
            }
        });
    }

    private static string BuildHttpError(HttpResponseMessage response, string? body)
    {
        var builder = new StringBuilder()
            .Append("HTTP ")
            .Append((int)response.StatusCode)
            .Append(' ')
            .Append(response.ReasonPhrase ?? "Unknown");

        if (!string.IsNullOrWhiteSpace(body))
        {
            builder.Append(": ").Append(body.Trim());
        }

        return builder.ToString();
    }
}
