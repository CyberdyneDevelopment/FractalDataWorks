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
    // Why a list keyed on method as well as url: the API gives list and create the same path and
    // separates them by verb, so a url-only stub for the create is shadowed by the one for the
    // list and the caller is handed an array where it expects an object. Entries are matched in
    // the order they were registered, and a stub naming a method only answers that method.
    private readonly List<(HttpMethod? Method, string UrlContains, string Json, HttpStatusCode Status)> _responses = [];

    private HttpStatusCode _defaultStatus = HttpStatusCode.NotFound;
    private string _defaultBody = string.Empty;

    /// <summary>Records every request received, so tests can assert which verbs/URLs were hit.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>
    /// Registers a JSON response for a specific URL pattern (substring match).
    /// </summary>
    public MockHttpHandler RespondWith<T>(string urlContains, T body, HttpStatusCode statusCode = HttpStatusCode.OK)
        => RespondWith(null, urlContains, body, statusCode);

    /// <summary>
    /// Registers a JSON response for a URL pattern reached by a specific method.
    /// </summary>
    public MockHttpHandler RespondWith<T>(HttpMethod? method, string urlContains, T body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Set(method, urlContains, json, statusCode);
        return this;
    }

    /// <summary>
    /// Registers a successful empty response for a specific URL pattern.
    /// </summary>
    public MockHttpHandler RespondOk(string urlContains)
    {
        Set(null, urlContains, "{}", HttpStatusCode.OK);
        return this;
    }

    /// <summary>
    /// Registers a failure response for a specific URL pattern.
    /// </summary>
    public MockHttpHandler RespondError(string urlContains, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        => RespondError(null, urlContains, statusCode);

    /// <summary>
    /// Registers a failure response for a URL pattern reached by a specific method.
    /// </summary>
    public MockHttpHandler RespondError(HttpMethod? method, string urlContains, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        Set(method, urlContains, "{\"message\":\"error\"}", statusCode);
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

    // Why replace rather than append on a repeated pattern: a test that registers a pattern and
    // then re-registers it with a failure is saying "this one fails", and appending would leave the
    // first, success-shaped stub in front of it.
    private void Set(HttpMethod? method, string urlContains, string json, HttpStatusCode status)
    {
        var existing = _responses.FindIndex(r => r.Method == method
            && string.Equals(r.UrlContains, urlContains, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0) _responses[existing] = (method, urlContains, json, status);
        else _responses.Add((method, urlContains, json, status));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var url = request.RequestUri?.ToString() ?? string.Empty;

        foreach (var stub in _responses)
        {
            if ((stub.Method is null || stub.Method == request.Method)
                && url.Contains(stub.UrlContains, StringComparison.OrdinalIgnoreCase))
            {
                var (json, status) = (stub.Json, stub.Status);
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
