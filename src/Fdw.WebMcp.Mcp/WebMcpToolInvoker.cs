using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;

namespace Fdw.WebMcp.Mcp;

/// <summary>
/// Runs one tool call by making the HTTP request the tool's plan describes against this host's own
/// route.
/// </summary>
/// <remarks>
/// The call goes to the tool's real route, so authentication, validation, filters and error shapes
/// are whatever that endpoint already does -- identical to a browser agent's call by construction,
/// rather than by a second implementation that agrees for now.
/// </remarks>
public sealed class WebMcpToolInvoker
{
    /// <summary>The name of the <see cref="HttpClient"/> this invoker sends on.</summary>
    public const string ClientName = "Fdw.WebMcp.Mcp";

    private readonly WebMcpMcpCatalogue _catalogue;
    private readonly IHttpClientFactory _clients;
    private readonly WebMcpHostSettingsReader _settings;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<WebMcpToolInvoker> _logger;

    /// <summary>Initializes the invoker.</summary>
    /// <param name="catalogue">The tools this host serves.</param>
    /// <param name="clients">Supplies the client tool calls are sent on, to absolute addresses under the host's own.</param>
    /// <param name="settings">The host's WebMCP settings: its own address, and the client key a tool call carries.</param>
    /// <param name="httpContext">Gives access to the calling agent's own request, to forward its identity.</param>
    /// <param name="logger">The logger.</param>
    public WebMcpToolInvoker(
        WebMcpMcpCatalogue catalogue,
        IHttpClientFactory clients,
        WebMcpHostSettingsReader settings,
        IHttpContextAccessor httpContext,
        ILogger<WebMcpToolInvoker>? logger = null)
    {
        _catalogue = catalogue;
        _clients = clients;
        _settings = settings;
        _httpContext = httpContext;
        _logger = logger ?? NullLogger<WebMcpToolInvoker>.Instance;
    }

    /// <summary>Calls one tool.</summary>
    /// <param name="toolName">The tool the agent named.</param>
    /// <param name="arguments">What the agent supplied.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The tool result, carrying the route's own response.</returns>
    public async Task<CallToolResult> Invoke(
        string toolName,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken = default)
    {
        var plan = _catalogue.PlanFor(toolName);
        if (plan is null)
        {
            return Failed(McpBridgeLog.ToolNotFound(_logger, toolName));
        }

        var built = WebMcpRequestBuilder.Build(plan, arguments, _logger);
        if (!built.IsSuccess || built.Value is not { } request)
        {
            return Failed(built);
        }

        var settings = await _settings.Read(cancellationToken).ConfigureAwait(false);
        if (settings.IsFailure)
        {
            return Failed(settings);
        }

        var host = settings.Value!;
        if (host.SelfBaseAddress is not { } self)
        {
            return Failed(McpBridgeLog.SelfBaseAddressMissing(_logger));
        }

        var client = _clients.CreateClient(ClientName);
        var target = new Uri(self, request.RelativeUrl);
        McpBridgeLog.DispatchingToolCall(_logger, toolName, request.Method, request.RelativeUrl);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(Compose(request, plan, target, host), cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            // A browser agent's equivalent call is same-origin and would have arrived; this one did
            // not, so the fault is the configured address or this host's reachability, not the caller.
            return Failed(McpBridgeLog.SelfRouteUnreachable(_logger, toolName, target.ToString(), exception.Message));
        }

        using (response)
        {
            McpBridgeLog.ToolCallCompleted(_logger, toolName, (int)response.StatusCode);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new CallToolResult { Content = [new TextContentBlock { Text = payload }] };
            }

            return FailedWith(await Failure(plan, response, client, self, host, cancellationToken).ConfigureAwait(false));
        }
    }

    /// <summary>Builds the request, carrying the same headers a browser agent's call carries.</summary>
    private HttpRequestMessage Compose(WebMcpToolRequest request, WebMcpToolPlan plan, Uri target, WebMcpHostSettings host)
    {
        var message = new HttpRequestMessage(new HttpMethod(request.Method), target);

        if (request.Body is not null)
        {
            // The content carries its own Content-Type, which is why that header is skipped below:
            // HttpClient refuses a content header set on the request.
            message.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
        }

        Apply(message, plan.HasBody, host);
        return message;
    }

    /// <summary>Applies the headers for a call and forwards the calling agent's own identity.</summary>
    private void Apply(HttpRequestMessage message, bool withBody, WebMcpHostSettings host)
    {
        foreach (var header in WebMcpRequestHeaders.For(withBody, host.ApiKeyHeader, host.ClientApiKey))
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Forwarded so the tool runs as the agent that asked for it. Never widened: a call carries
        // what its caller presented and nothing more, which is also what the recovery call below
        // reuses, so an agent can never be shown values it could not have listed itself.
        if (_httpContext.HttpContext?.Request.Headers.Authorization is { Count: > 0 } authorization)
        {
            message.Headers.TryAddWithoutValidation("Authorization", (string?)authorization);
        }
    }

    /// <summary>The failure an agent is given, with the values its parameter selects from when there are any.</summary>
    private async Task<string> Failure(
        WebMcpToolPlan plan,
        HttpResponseMessage response,
        HttpClient client,
        Uri self,
        WebMcpHostSettings host,
        CancellationToken cancellationToken)
    {
        var failure = new JsonObject
        {
            ["error"] = FormattableString.Invariant($"{(int)response.StatusCode} {response.ReasonPhrase}"),
        };

        // A bare 404 is a dead end: an agent cannot tell a wrong identifier from an empty result,
        // and nothing in the response says what a right one would look like.
        if (plan.RecoveryProperty is { } parameter && plan.Tool.ParentListRoute is { } parentRoute)
        {
            McpBridgeLog.OfferingRecovery(_logger, plan.Tool.Name, parentRoute);

            try
            {
                var listing = new HttpRequestMessage(HttpMethod.Get, new Uri(self, parentRoute));
                Apply(listing, withBody: false, host);

                using var alternative = await client.SendAsync(listing, cancellationToken).ConfigureAwait(false);
                if (alternative.IsSuccessStatusCode)
                {
                    failure["validValues"] = JsonNode.Parse(
                        await alternative.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                    failure["hint"] = FormattableString.Invariant(
                        $"'{parameter}' did not match. failure.validValues lists what is available; '{plan.Tool.ParentListToolName}' returns the same set.");
                }
            }
            catch (HttpRequestException exception)
            {
                // Recovery is best-effort, exactly as it is in the script: the original failure
                // still returns, and an unreachable listing must not replace it.
                McpBridgeLog.RecoveryUnavailable(_logger, plan.Tool.Name, exception.Message);
            }
            catch (JsonException exception)
            {
                // A listing that is not JSON cannot be offered as valid values; the failure stands.
                McpBridgeLog.RecoveryUnavailable(_logger, plan.Tool.Name, exception.Message);
            }
        }

        return failure.ToJsonString();
    }

    /// <summary>The error an agent is given, from a single message.</summary>
    private static CallToolResult Failed(IGenericMessage message)
        => FailedWith(message.Message);

    /// <summary>The error an agent is given, from a failed result.</summary>
    private static CallToolResult Failed(IGenericResult result)
        => FailedWith(string.Join("; ", result.Messages.Select(message => message.Message)));

    private static CallToolResult FailedWith(string text)
        => new() { IsError = true, Content = [new TextContentBlock { Text = text }] };
}
