using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>
/// What one scenario shares between its steps: the person asking, the last answer and the containers it wrote to, so the
/// after-scenario hook removes what the scenario added. Reqnroll gives each scenario its own instance.
/// </summary>
internal sealed class ScenarioState : System.IDisposable
{
    private readonly HttpClient _client = new();
    private readonly HashSet<string> _writtenContainers = new(System.StringComparer.Ordinal);

    internal Permission? Person { get; set; }

    internal bool UseHostWithoutTable { get; set; }

    internal Answer? Last { get; set; }

    internal Answer Answered => Last ?? throw new System.InvalidOperationException("The scenario checks an answer before it asks for one.");

    internal IReadOnlyCollection<string> WrittenContainers => _writtenContainers;

    public void Dispose() => _client.Dispose();

    internal void Wrote(string container) => _writtenContainers.Add(container);

    internal static string ContainerBase(string container) =>
        $"/datastores/{ScenarioHost.Required(ScenarioHost.DataStoreVariable)}/paths/{ScenarioHost.Required(ScenarioHost.PathVariable)}/containers/{container}";

    internal Task<Answer> SendAsync(HttpMethod method, string relativeUrl, string? json, CancellationToken cancellationToken) =>
        Person is { } person
            ? SendAsAsync(person, method, relativeUrl, json, cancellationToken)
            : throw new System.InvalidOperationException("The scenario sends a request before it says who asks. Start it with a step that names a person.");

    internal async Task<Answer> SendAsAsync(Permission person, HttpMethod method, string relativeUrl, string? json, CancellationToken cancellationToken)
    {
        var host = ScenarioHost.Required(UseHostWithoutTable ? ScenarioHost.AddressWithoutTableVariable : ScenarioHost.AddressVariable);
        using var request = new HttpRequestMessage(method, new System.Uri(new System.Uri(host), relativeUrl));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ScenarioHost.Required(ScenarioHost.TokenVariable(person)));
        if (json is not null)
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return new Answer((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
    }
}
