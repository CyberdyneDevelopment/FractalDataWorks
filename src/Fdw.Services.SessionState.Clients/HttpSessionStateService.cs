using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.SessionState.Clients.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SessionState.Clients;

/// <summary>
/// HTTP-backed implementation of <see cref="ISessionStateService"/> for use in the UI.
/// Delegates to the session state API endpoints instead of accessing the database directly.
/// The userId parameter is derived from the bearer token on the server side and is not sent explicitly.
/// </summary>
public sealed class HttpSessionStateService : ISessionStateService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpSessionStateService> _logger;

    /// <summary>Initializes a new instance of <see cref="HttpSessionStateService"/>.</summary>
    public HttpSessionStateService(IHttpClientFactory httpClientFactory, ILogger<HttpSessionStateService>? logger = null)
    {
        _httpClient = httpClientFactory.CreateClient("SessionStateApi");
        _logger = logger ?? NullLogger<HttpSessionStateService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> SaveState<T>(string userId, string key, T value, CancellationToken cancellationToken = default)
    {
        var body = new { Value = JsonSerializer.SerializeToElement(value, SerializerOptions) };
        // Why PATCH: the upsert endpoint moved from PUT to PATCH in the verb sweep (4ffe4acfc)
        // and this caller was not moved with it, so every save answered 405.
        var response = await _httpClient.PatchAsJsonAsync($"session-state/{Uri.EscapeDataString(key)}", body, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return GenericResult<bool>.Failure(SessionStateClientLog.SaveFailed(_logger, key, (int)response.StatusCode));

        return GenericResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<T?>> GetState<T>(string userId, string key, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"session-state/{Uri.EscapeDataString(key)}", cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return GenericResult<T?>.Success(default);

        if (!response.IsSuccessStatusCode)
            return GenericResult<T?>.Failure(SessionStateClientLog.GetFailed(_logger, key, (int)response.StatusCode));

        var entry = await response.Content.ReadFromJsonAsync<SessionStateEntryResponse>(cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return GenericResult<T?>.Success(default);

        var value = entry.Value.Deserialize<T>(SerializerOptions);
        return GenericResult<T?>.Success(value);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> DeleteState(string userId, string key, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"session-state/{Uri.EscapeDataString(key)}", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            return GenericResult<bool>.Failure(SessionStateClientLog.DeleteFailed(_logger, key, (int)response.StatusCode));

        return GenericResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<string>>> GetAllKeys(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("session-state", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return GenericResult<IReadOnlyList<string>>.Failure(SessionStateClientLog.GetAllKeysFailed(_logger, (int)response.StatusCode));

        var result = await response.Content.ReadFromJsonAsync<SessionStateKeysResponse>(cancellationToken).ConfigureAwait(false);
        return GenericResult<IReadOnlyList<string>>.Success(result?.Keys ?? []);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> ClearAll(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync("session-state", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return GenericResult<bool>.Failure(SessionStateClientLog.ClearAllFailed(_logger, (int)response.StatusCode));

        return GenericResult<bool>.Success(true);
    }

    private sealed class SessionStateEntryResponse
    {
        public string Key { get; set; } = string.Empty;
        public JsonElement Value { get; set; }
    }

    private sealed class SessionStateKeysResponse
    {
        public IReadOnlyList<string> Keys { get; set; } = [];
    }
}
