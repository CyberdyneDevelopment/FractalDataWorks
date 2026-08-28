namespace Fdw.Web.Clients.Abstractions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Base class for API clients providing standard HTTP operations with
/// integrated structured logging and GenericResult error handling.
/// </summary>
public abstract class ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientBase"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    protected ApiClientBase(HttpClient httpClient, ILogger<ApiClientBase>? logger = null)
    {
        HttpClient = httpClient;
        Logger = logger ?? NullLogger<ApiClientBase>.Instance;
    }

    /// <summary>
    /// Gets the HTTP client.
    /// </summary>
    protected HttpClient HttpClient { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Resolves the absolute request URI for a relative path.
    /// </summary>
    /// <param name="path">The relative request path.</param>
    /// <returns>The absolute URI when a base address is configured; otherwise the path as-is.</returns>
    /// <remarks>
    /// Why: every ClientLog message reports the ABSOLUTE target — a relative path alone hides which
    /// host/port the client actually hit, which is exactly the detail needed when a misconfigured
    /// BaseAddress is the fault. No base address configured is itself diagnostic, so the bare path
    /// (not an invented host) is reported in that case.
    /// </remarks>
    protected string RequestUri(string path)
        => HttpClient.BaseAddress is null ? path : new Uri(HttpClient.BaseAddress, path).ToString();

    /// <summary>
    /// Performs a GET request and deserializes the response.
    /// </summary>
    protected async Task<IGenericResult<T>> Get<T>(string path, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "GET", RequestUri(path));
        try
        {
            var result = await HttpClient.GetFromJsonAsync<T>(path, SharedJsonOptions, ct).ConfigureAwait(false);
            if (result is null)
            {
                return GenericResult<T>.Failure(
                    ClientLog.NullResponseBody(Logger, "GET", RequestUri(path)));
            }

            ClientLog.RequestCompleted(Logger, "GET", RequestUri(path));
            return GenericResult<T>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<T>.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "GET", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult<T>.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "GET", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                ClientLog.UnexpectedError(Logger, ex, "GET", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a GET request for a list endpoint, handling both paginated envelope
    /// (<c>{"items":[...]}</c>) and flat array (<c>[...]</c>) response formats.
    /// </summary>
    protected async Task<IGenericResult<IReadOnlyList<T>>> GetList<T>(string path, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "GET", RequestUri(path));
        try
        {
            var response = await HttpClient.GetAsync(path, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return GenericResult<IReadOnlyList<T>>.Failure(
                    await NonSuccessDetail("GET", path, response, ct).ConfigureAwait(false));
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            IReadOnlyList<T>? items;
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                items = doc.RootElement.Deserialize<IReadOnlyList<T>>(SharedJsonOptions);
            }
            else if (doc.RootElement.TryGetProperty("items", out var itemsElement))
            {
                items = itemsElement.Deserialize<IReadOnlyList<T>>(SharedJsonOptions);
            }
            else
            {
                return GenericResult<IReadOnlyList<T>>.Failure(
                    ClientLog.ResponseShapeUnrecognized(Logger, "GET", RequestUri(path)));
            }

            ClientLog.RequestCompleted(Logger, "GET", RequestUri(path));
            return GenericResult<IReadOnlyList<T>>.Success(items ?? []);
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<IReadOnlyList<T>>.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "GET", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult<IReadOnlyList<T>>.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "GET", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<T>>.Failure(
                ClientLog.UnexpectedError(Logger, ex, "GET", RequestUri(path), ex.Message));
        }
    }

    private static readonly JsonSerializerOptions SharedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Builds the failure message for a non-success HTTP response, folding in the server's
    /// response body so the real reason survives to the caller.
    /// </summary>
    private async Task<IGenericMessage> NonSuccessDetail(string method, string path, HttpResponseMessage response, CancellationToken ct)
    {
        string detail;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            detail = string.IsNullOrWhiteSpace(body)
                ? $"status {(int)response.StatusCode}"
                : DescribeBody(body, (int)response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            detail = $"status {(int)response.StatusCode}: {ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            detail = $"status {(int)response.StatusCode}: {ex.Message}";
        }

        return ClientLog.RequestNonSuccessDetail(Logger, method, RequestUri(path), (int)response.StatusCode, detail);
    }

    /// <summary>
    /// Performs a POST request with a request body and deserializes the response.
    /// </summary>
    protected async Task<IGenericResult<TResponse>> Post<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "POST", RequestUri(path));
        try
        {
            var response = await HttpClient.PostAsJsonAsync(path, request, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "POST", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(SharedJsonOptions, ct).ConfigureAwait(false);
                if (result is null)
                {
                    return GenericResult<TResponse>.Failure(
                        ClientLog.RequestNonSuccess(Logger, "POST", RequestUri(path), (int)response.StatusCode));
                }

                ClientLog.RequestCompleted(Logger, "POST", RequestUri(path));
                return GenericResult<TResponse>.Success(result);
            }

            return GenericResult<TResponse>.Failure(
                await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.UnexpectedError(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a POST request with a request body, returning success/failure without a response body.
    /// </summary>
    protected async Task<IGenericResult> Post<TRequest>(string path, TRequest request, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "POST", RequestUri(path));
        try
        {
            var response = await HttpClient.PostAsJsonAsync(path, request, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "POST", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                ClientLog.RequestCompleted(Logger, "POST", RequestUri(path));
                return GenericResult.Success();
            }

            return GenericResult.Failure(
                await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ClientLog.UnexpectedError(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a POST request without a request or response body.
    /// </summary>
    protected async Task<IGenericResult> Post(string path, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "POST", RequestUri(path));
        try
        {
            using var emptyContent = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(path, emptyContent, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "POST", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                ClientLog.RequestCompleted(Logger, "POST", RequestUri(path));
                return GenericResult.Success();
            }

            return GenericResult.Failure(
                await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ClientLog.UnexpectedError(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a POST request without a request body, deserializing the response.
    /// </summary>
    protected async Task<IGenericResult<TResponse>> PostWithResponse<TResponse>(string path, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "POST", RequestUri(path));
        try
        {
            using var emptyContent = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(path, emptyContent, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "POST", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(SharedJsonOptions, ct).ConfigureAwait(false);
                if (result is null)
                {
                    return GenericResult<TResponse>.Failure(
                        ClientLog.RequestNonSuccess(Logger, "POST", RequestUri(path), (int)response.StatusCode));
                }

                ClientLog.RequestCompleted(Logger, "POST", RequestUri(path));
                return GenericResult<TResponse>.Success(result);
            }

            return GenericResult<TResponse>.Failure(
                await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.UnexpectedError(Logger, ex, "POST", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a PUT request with a request body and deserializes the response.
    /// </summary>
    protected async Task<IGenericResult<TResponse>> Put<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "PUT", RequestUri(path));
        try
        {
            var response = await HttpClient.PutAsJsonAsync(path, request, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "PUT", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(SharedJsonOptions, ct).ConfigureAwait(false);
                if (result is null)
                {
                    return GenericResult<TResponse>.Failure(
                        ClientLog.RequestNonSuccess(Logger, "PUT", RequestUri(path), (int)response.StatusCode));
                }

                ClientLog.RequestCompleted(Logger, "PUT", RequestUri(path));
                return GenericResult<TResponse>.Success(result);
            }

            return GenericResult<TResponse>.Failure(
                await NonSuccessDetail("PUT", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "PUT", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "PUT", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.UnexpectedError(Logger, ex, "PUT", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a PUT request with a request body, returning success/failure without a response body.
    /// </summary>
    protected async Task<IGenericResult> Put<TRequest>(string path, TRequest request, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "PUT", RequestUri(path));
        try
        {
            var response = await HttpClient.PutAsJsonAsync(path, request, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "PUT", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                ClientLog.RequestCompleted(Logger, "PUT", RequestUri(path));
                return GenericResult.Success();
            }

            return GenericResult.Failure(
                await NonSuccessDetail("PUT", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "PUT", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "PUT", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ClientLog.UnexpectedError(Logger, ex, "PUT", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a PATCH request with a request body, returning success/failure without a response body.
    /// </summary>
    protected async Task<IGenericResult> Patch<TRequest>(string path, TRequest request, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "PATCH", RequestUri(path));
        try
        {
            var response = await HttpClient.PatchAsync(path, JsonContent.Create(request), ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "PATCH", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                ClientLog.RequestCompleted(Logger, "PATCH", RequestUri(path));
                return GenericResult.Success();
            }

            return GenericResult.Failure(
                await NonSuccessDetail("PATCH", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "PATCH", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "PATCH", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ClientLog.UnexpectedError(Logger, ex, "PATCH", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a PATCH request with a request body and deserializes the response.
    /// </summary>
    protected async Task<IGenericResult<TResponse>> Patch<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "PATCH", RequestUri(path));
        try
        {
            var content = JsonContent.Create(request);
            var response = await HttpClient.PatchAsync(path, content, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "PATCH", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(SharedJsonOptions, ct).ConfigureAwait(false);
                if (result is null)
                {
                    return GenericResult<TResponse>.Failure(
                        ClientLog.RequestNonSuccess(Logger, "PATCH", RequestUri(path), (int)response.StatusCode));
                }

                ClientLog.RequestCompleted(Logger, "PATCH", RequestUri(path));
                return GenericResult<TResponse>.Success(result);
            }

            return GenericResult<TResponse>.Failure(
                await NonSuccessDetail("PATCH", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "PATCH", RequestUri(path), ex.Message));
        }
        catch (JsonException ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.DeserializationFailed(Logger, ex, "PATCH", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<TResponse>.Failure(
                ClientLog.UnexpectedError(Logger, ex, "PATCH", RequestUri(path), ex.Message));
        }
    }

    /// <summary>
    /// Performs a DELETE request.
    /// </summary>
    protected async Task<IGenericResult> Delete(string path, CancellationToken ct = default)
    {
        ClientLog.SendingRequest(Logger, "DELETE", RequestUri(path));
        try
        {
            var response = await HttpClient.DeleteAsync(path, ct).ConfigureAwait(false);
            ClientLog.ResponseReceived(Logger, "DELETE", RequestUri(path), (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                ClientLog.RequestCompleted(Logger, "DELETE", RequestUri(path));
                return GenericResult.Success();
            }

            return GenericResult.Failure(
                await NonSuccessDetail("DELETE", path, response, ct).ConfigureAwait(false));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult.Failure(
                ClientLog.HttpRequestFailed(Logger, ex, "DELETE", RequestUri(path), ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ClientLog.UnexpectedError(Logger, ex, "DELETE", RequestUri(path), ex.Message));
        }
    }

    /// <summary>Turns a response body into one readable sentence.</summary>
    /// <param name="body">The raw response body.</param>
    /// <param name="statusCode">The HTTP status, used when the body says nothing useful.</param>
    /// <returns>The description.</returns>
    private static string DescribeBody(string body, int statusCode)
    {
        // Why: a non-JSON body is the ordinary case here, not a failure, so this probes with a
        // non-throwing parse rather than catching JsonException. Catching it made a routine outcome
        // look like a swallowed error and cost an exception on every plain-text response.
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(body));
        if (JsonDocument.TryParseValue(ref reader, out var doc))
        {
            using (doc)
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var text = doc.RootElement.TryGetProperty("detail", out var d) ? d.GetString() : null;
                    if (string.IsNullOrWhiteSpace(text) && doc.RootElement.TryGetProperty("title", out var t))
                    {
                        text = t.GetString();
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return doc.RootElement.TryGetProperty("code", out var c) && c.GetString() is string codeText
                            ? $"{text} ({codeText})"
                            : text;
                    }
                }
            }
        }

        return (body.Length > 500 ? body[..500] : body).Trim();
    }
}
