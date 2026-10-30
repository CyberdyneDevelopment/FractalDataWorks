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
using Fdw.Web.Clients.Abstractions.Results;
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
            // Why branch on StatusCode: GetFromJsonAsync throws on any non-success response, with
            // .NET populating StatusCode from that response since .NET 5 -- a null StatusCode is
            // the transport-level failure (no response at all), which carries no status to map.
            if (ex.StatusCode.HasValue)
            {
                var code = ClientResultCodes.ByName(StatusCode(ex.StatusCode.Value));
                var details = ResultDetails.Create()
                    .With("method", "GET").With("uri", RequestUri(path))
                    .With("statusCode", (int)ex.StatusCode.Value).With("detail", ex.Message);
                return GenericResult<T>.Failure(code, Logger, details);
            }

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
                return (await NonSuccessDetail("GET", path, response, ct).ConfigureAwait(false)).ToNewResult<IReadOnlyList<T>>();
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

            // A streamed page writes its error after its items: the status was already 200 when the
            // server found it could not finish the page.
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind != JsonValueKind.Null)
            {
                return GenericResult<IReadOnlyList<T>>.Failure(
                    ClientLog.PageEndedEarly(Logger, "GET", RequestUri(path), error.GetRawText()));
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
    /// Builds a coded failure for a non-success HTTP response, folding in the server's response
    /// body so the real reason survives to the caller, and carrying the actual status band so a
    /// caller downstream (a proxy endpoint, for instance) can answer with the status the server
    /// really sent instead of one fixed status for every kind of failure.
    /// </summary>
    private async Task<IGenericResult> NonSuccessDetail(string method, string path, HttpResponseMessage response, CancellationToken ct)
    {
        string detail;
        string? reason;
        string? upstreamReferenceId;
        string? upstreamCode;
        string? executionId;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var document = ObjectBody(body, method, path);
            detail = string.IsNullOrWhiteSpace(body)
                ? $"status {(int)response.StatusCode}"
                : DescribeBody(document, body);
            // A problem body states its reason in "detail"; a refused trigger answers a body of its own that states it in "reason".
            reason = document is null ? null : StringOf(document.RootElement, "detail") is { } stated ? stated : StringOf(document.RootElement, "reason");
            upstreamReferenceId = document is null ? null : StringOf(document.RootElement, "referenceId");
            upstreamCode = document is null ? null : StringOf(document.RootElement, "code");
            executionId = document is null ? null : StringOf(document.RootElement, ResultDetailKeys.ExecutionId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return ChainedNonSuccess(response, method, path, ex);
        }
        catch (HttpRequestException ex)
        {
            return ChainedNonSuccess(response, method, path, ex);
        }

        var code = ClientResultCodes.ByName(StatusCode(response.StatusCode));
        var details = ResultDetails.Create()
            .With("method", method).With("uri", RequestUri(path))
            .With("statusCode", (int)response.StatusCode).With("detail", detail).With("reason", reason)
            .With(ResultDetailKeys.UpstreamReferenceId, upstreamReferenceId).With(ResultDetailKeys.UpstreamCode, upstreamCode)
            .With(ResultDetailKeys.ExecutionId, executionId);
        return GenericResult.Failure(code, Logger, details);
    }

    // The body as a JSON object, or null when it is not one. Only text that starts with '{' can carry a problem body's detail or
    // title: plain text, an HTML page from a proxy and a bare JSON value are ordinary upstream answers that give no reason, so
    // they are recognised without parsing and without an exception. Text that starts like an object but does not parse is logged
    // and gives no reason either; the coded failure for the upstream's status is kept in every case.
    private JsonDocument? ObjectBody(string body, string method, string path)
    {
        if (!body.TrimStart().StartsWith('{'))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            ClientLog.UpstreamBodyNotJson(Logger, ex, method, RequestUri(path), ex.Message);
            return null;
        }
    }

    // A property read as text only when it is text: an upstream that sends a number or an object there is not a reason, and
    // reading it as a string would throw.
    private static string? StringOf(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>
    /// Builds the coded non-success failure when reading the response body itself threw --
    /// the exception is chained as the inner result rather than folded into a string, so it
    /// survives on the result the same way any other caught exception does here.
    /// </summary>
    private IGenericResult ChainedNonSuccess(HttpResponseMessage response, string method, string path, Exception ex)
    {
        var code = ClientResultCodes.ByName(StatusCode(response.StatusCode));
        var details = ResultDetails.Create()
            .With("method", method).With("uri", RequestUri(path))
            .With("statusCode", (int)response.StatusCode).With("detail", ex.Message);
        var inner = GenericResult.Failure(ClientLog.HttpRequestFailed(Logger, ex, method, RequestUri(path), ex.Message));
        return GenericResult.Chain(code, inner, Logger, details);
    }

    /// <summary>Names the <see cref="ClientResultCodes"/> option matching an HTTP status.</summary>
    /// <remarks>
    /// Only the bands this client has actually seen a downstream FDW host answer get their own
    /// name; everything else (an unrecognized 4xx, a 502) falls to UpstreamCallFailed, which is
    /// the single fixed status every non-success used to answer with. A 500 has its own name,
    /// UpstreamInternalError, because a host that relays it must answer 500 as the downstream did:
    /// answering 502 named the relaying host as the one that failed, and a browser behind a CDN
    /// then received the CDN's page in place of the downstream's problem body.
    /// </remarks>
    private static string StatusCode(System.Net.HttpStatusCode statusCode) => (int)statusCode switch
    {
        400 => "UpstreamRequestInvalid",
        401 => "UpstreamAuthenticationFailed",
        403 => "UpstreamForbidden",
        404 => "UpstreamResourceNotFound",
        409 => "UpstreamConflict",
        500 => "UpstreamInternalError",
        503 => "UpstreamUnavailable",
        504 => "UpstreamGatewayTimeout",
        _ => "UpstreamCallFailed",
    };

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

            return (await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false)).ToNewResult<TResponse>();
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

            return await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false);
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

            return await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false);
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

            return (await NonSuccessDetail("POST", path, response, ct).ConfigureAwait(false)).ToNewResult<TResponse>();
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

            return (await NonSuccessDetail("PUT", path, response, ct).ConfigureAwait(false)).ToNewResult<TResponse>();
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

            return await NonSuccessDetail("PUT", path, response, ct).ConfigureAwait(false);
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

            return await NonSuccessDetail("PATCH", path, response, ct).ConfigureAwait(false);
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

            return (await NonSuccessDetail("PATCH", path, response, ct).ConfigureAwait(false)).ToNewResult<TResponse>();
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

            return await NonSuccessDetail("DELETE", path, response, ct).ConfigureAwait(false);
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
    /// <param name="document">The body parsed as a JSON object, or null when it is not one.</param>
    /// <param name="body">The raw response body.</param>
    /// <returns>The description.</returns>
    private static string DescribeBody(JsonDocument? document, string body)
    {
        if (document is not null)
        {
            var text = StringOf(document.RootElement, "detail");
            if (string.IsNullOrWhiteSpace(text))
            {
                text = StringOf(document.RootElement, "title");
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                return StringOf(document.RootElement, "code") is string codeText
                    ? $"{text} ({codeText})"
                    : text;
            }
        }

        return (body.Length > 500 ? body[..500] : body).Trim();
    }
}
