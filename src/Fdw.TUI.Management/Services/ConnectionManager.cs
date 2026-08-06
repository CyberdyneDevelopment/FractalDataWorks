using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Tracks saved Fdw instances and the currently connected one, verifying reachability over HTTP.
/// </summary>
/// <remarks>
/// The saved-instance list is client-side state by design (see <see cref="IConnectionManager"/>).
/// Connecting is a real probe against the instance's health endpoint — it is not assumed.
/// </remarks>
public sealed class ConnectionManager : IConnectionManager
{
    /// <summary>Named client used only for the connect-time probe; the API clients route separately.</summary>
    public const string ProbeClientName = "InstanceProbe";

    private const string HealthPath = "health";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly List<SavedConnection> _savedConnections = new();
    private ConnectionStatus _status = new() { IsConnected = false };
    private SavedConnection? _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
    /// </summary>
    public ConnectionManager(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc />
    public ConnectionStatus GetStatus() => _status;

    /// <inheritdoc />
    public SavedConnection? GetCurrentConnection() => _current;

    /// <inheritdoc />
    public IReadOnlyList<SavedConnection> GetSavedConnections() => _savedConnections.AsReadOnly();

    /// <inheritdoc />
    public void SaveConnection(SavedConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // Remove existing connection with same name
        _savedConnections.RemoveAll(c => string.Equals(c.Name, connection.Name, StringComparison.OrdinalIgnoreCase));

        _savedConnections.Add(connection);
    }

    /// <inheritdoc />
    public void RemoveConnection(string name)
    {
        _savedConnections.RemoveAll(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<ConnectionResult> Connect(SavedConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(connection.Url))
        {
            return ConnectionResult.Failed("URL is required");
        }

        if (!Uri.TryCreate(NormalizeBase(connection.Url), UriKind.Absolute, out var baseUri))
        {
            return ConnectionResult.Failed("Invalid URL format");
        }

        if (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return ConnectionResult.Failed("URL must use http or https scheme");
        }

        // Why: actually reach the instance before declaring ourselves connected. Previously this method
        // slept and assumed success, so every screen rendered against a server that may not exist.
        var probe = await Probe(connection, baseUri, cancellationToken).ConfigureAwait(false);
        if (!probe.Success)
        {
            return probe;
        }

        connection.LastUsed = DateTime.Now;
        _current = connection;
        _status = new ConnectionStatus
        {
            IsConnected = true,
            InstanceName = connection.Name,
            Url = connection.Url,
            ConnectedAt = DateTime.Now
        };

        return ConnectionResult.Succeeded();
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        _current = null;
        _status = new ConnectionStatus { IsConnected = false };
    }

    /// <summary>
    /// Normalizes a base URL so a relative path appends to it rather than replacing its last segment.
    /// </summary>
    internal static string NormalizeBase(string url) =>
        url.EndsWith('/') ? url : url + "/";

    private async Task<ConnectionResult> Probe(SavedConnection connection, Uri baseUri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, HealthPath));
        if (!string.IsNullOrEmpty(connection.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);
        }

        var client = _httpClientFactory.CreateClient(ProbeClientName);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return ConnectionResult.Failed("Authentication was rejected — check the API key.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ConnectionResult.Failed(
                    $"Instance answered {(int)response.StatusCode} from /{HealthPath}.");
            }

            return ConnectionResult.Succeeded();
        }
        catch (HttpRequestException ex)
        {
            return ConnectionResult.Failed($"Could not reach {baseUri}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Why the filter: HttpClient reports its own timeout as TaskCanceledException, so this
            // distinguishes "the instance did not answer in time" from "the user cancelled".
            return ConnectionResult.Failed($"Timed out connecting to {baseUri} ({ex.Message}).");
        }
    }
}
