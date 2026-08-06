using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.UI.Components.Blazor.Tests.Helpers;

/// <summary>
/// A mock HTTP message handler that returns preconfigured JSON responses.
/// Used for testing providers that depend on sealed API clients.
/// Returns a fresh HttpResponseMessage on each request so content can be read multiple times.
/// </summary>
public sealed class MockHttpHandler : HttpMessageHandler
{
    // Why: Store the raw JSON + status code rather than a single HttpResponseMessage instance.
    // HttpResponseMessage.Content (StringContent) is a read-once stream; returning the same
    // instance on repeated requests causes empty/failed reads on the second call.
    private readonly Dictionary<string, (string Json, HttpStatusCode Status)> _responses
        = new(StringComparer.OrdinalIgnoreCase);

    private HttpStatusCode _defaultStatus = HttpStatusCode.NotFound;
    private string _defaultBody = string.Empty;

    /// <summary>Records every request received, so tests can assert which verbs/URLs were hit.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>
    /// Registers a JSON response for a specific URL pattern (substring match).
    /// </summary>
    public MockHttpHandler RespondWith<T>(string urlContains, T body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _responses[urlContains] = (json, statusCode);
        return this;
    }

    /// <summary>
    /// Registers a successful empty response for a specific URL pattern.
    /// </summary>
    public MockHttpHandler RespondOk(string urlContains)
    {
        _responses[urlContains] = ("{}", HttpStatusCode.OK);
        return this;
    }

    /// <summary>
    /// Registers a failure response for a specific URL pattern.
    /// </summary>
    public MockHttpHandler RespondError(string urlContains, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        _responses[urlContains] = ("{\"message\":\"error\"}", statusCode);
        return this;
    }

    /// <summary>
    /// Sets the default response for unmatched URLs.
    /// </summary>
    public MockHttpHandler WithDefault(HttpStatusCode statusCode)
    {
        _defaultStatus = statusCode;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var url = request.RequestUri?.ToString() ?? string.Empty;

        foreach (var kvp in _responses)
        {
            if (url.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                var (json, status) = kvp.Value;
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(_defaultStatus)
        {
            Content = new StringContent(_defaultBody, Encoding.UTF8, "application/json")
        });
    }
}
