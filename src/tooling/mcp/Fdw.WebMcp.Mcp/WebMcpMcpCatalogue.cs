using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Conventions;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;

namespace Fdw.WebMcp.Mcp;

/// <summary>
/// This host's WebMCP tools, described for an MCP client.
/// </summary>
/// <remarks>
/// The catalogue is the tool registry -- the same one <c>webmcp.js</c> is generated from -- and each
/// tool's schema comes from the same <see cref="WebMcpToolPlan"/> the script renders. A tool
/// offered through one transport and not the other, or described differently by them, would have to
/// be made possible by a change here AND there.
/// </remarks>
public sealed class WebMcpMcpCatalogue
{
    private readonly IWebMcpToolRegistry _registry;
    private readonly ILogger<WebMcpMcpCatalogue> _logger;
    private readonly ConcurrentDictionary<string, WebMcpToolPlan> _plans = new(StringComparer.Ordinal);

    /// <summary>Initializes the catalogue.</summary>
    /// <param name="registry">The resolved WebMCP tool registry.</param>
    /// <param name="logger">The logger.</param>
    public WebMcpMcpCatalogue(IWebMcpToolRegistry registry, ILogger<WebMcpMcpCatalogue>? logger = null)
    {
        _registry = registry;
        _logger = logger ?? NullLogger<WebMcpMcpCatalogue>.Instance;
    }

    /// <summary>The plan for one tool, or <see langword="null"/> when this host serves no such tool.</summary>
    /// <param name="toolName">The name an agent used.</param>
    /// <returns>The plan, or <see langword="null"/>.</returns>
    public WebMcpToolPlan? PlanFor(string toolName)
    {
        var descriptor = _registry.Tools.FirstOrDefault(
            tool => string.Equals(tool.Name, toolName, StringComparison.Ordinal));

        return descriptor is null
            ? null
            : _plans.GetOrAdd(descriptor.Name, _ => WebMcpToolPlan.Create(descriptor, _logger));
    }

    /// <summary>Every tool this host serves, as MCP describes them.</summary>
    /// <returns>The tool list.</returns>
    public IList<Tool> Tools()
    {
        McpBridgeLog.ServingCatalogue(_logger, _registry.Tools.Count);

        return _registry.Tools
            .Select(descriptor => Describe(_plans.GetOrAdd(descriptor.Name, _ => WebMcpToolPlan.Create(descriptor, _logger))))
            .ToList();
    }

    /// <summary>The tools a caller may call, as MCP describes them.</summary>
    /// <param name="context">The request the tool list was asked for in; its user is the caller.</param>
    /// <param name="cancellationToken">Cancels the judgement.</param>
    /// <returns>Each tool whose route's authorization admits the caller.</returns>
    /// <remarks>
    /// Each tool is judged as ASP.NET's authorization middleware judges a request to its route: an endpoint
    /// that allows anonymous callers is offered to everyone; otherwise the endpoint's authorize data and
    /// policies are combined, the host's fallback policy applies when the endpoint declares none, and the
    /// policy evaluator authenticates and authorizes the caller against it. A tool is offered only when that
    /// succeeds, so the list never shows a tool its route would refuse. A call to a withheld tool is still
    /// refused by the route itself; this only decides what is offered.
    /// </remarks>
    public async Task<IList<Tool>> ToolsFor(HttpContext? context, CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            McpBridgeLog.NoCallerToJudge(_logger);
            return [];
        }

        var policyProvider = context.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();
        var evaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();
        var caller = context.User;
        var offered = new List<Tool>();

        foreach (var descriptor in _registry.Tools)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (descriptor.Endpoint is null)
            {
                McpBridgeLog.ToolWithoutEndpoint(_logger, descriptor.Name);
                continue;
            }

            if (await Admits(descriptor.Endpoint, context, policyProvider, evaluator).ConfigureAwait(false))
            {
                offered.Add(Describe(_plans.GetOrAdd(descriptor.Name, _ => WebMcpToolPlan.Create(descriptor, _logger))));
            }
            else
            {
                McpBridgeLog.ToolWithheld(_logger, descriptor.Name, CallerName(caller));
            }

            // Why: the evaluator replaces the user for a policy that names its own schemes, and the next
            // tool must be judged against the caller as they arrived.
            context.User = caller;
        }

        McpBridgeLog.OfferingTools(_logger, offered.Count, _registry.Tools.Count, CallerName(caller));
        return offered;
    }

    private static async Task<bool> Admits(Endpoint endpoint, HttpContext context, IAuthorizationPolicyProvider policyProvider, IPolicyEvaluator evaluator)
    {
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return true;
        }

        var policy = await AuthorizationPolicy.CombineAsync(
                policyProvider,
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>())
            .ConfigureAwait(false);
        policy ??= await policyProvider.GetFallbackPolicyAsync().ConfigureAwait(false);
        if (policy is null)
        {
            return true;
        }

        var authenticated = await evaluator.AuthenticateAsync(policy, context).ConfigureAwait(false);
        var authorized = await evaluator.AuthorizeAsync(policy, authenticated, context, context).ConfigureAwait(false);
        return authorized.Succeeded;
    }

    private static string CallerName(ClaimsPrincipal caller)
        => caller.Identity is { IsAuthenticated: true, Name: { Length: > 0 } name } ? name : LogPlaceholders.Anonymous;

    private static Tool Describe(WebMcpToolPlan plan)
    {
        var tool = new Tool
        {
            Name = plan.Tool.Name,
            Description = plan.Tool.Description,
            InputSchema = InputSchema(plan),
        };

        // Only set when the tool declared itself read-only, matching the script, which emits the
        // annotation only then. An absent hint and a false one do not mean the same thing.
        if (plan.Tool.ReadOnly)
        {
            tool.Annotations = new ToolAnnotations { ReadOnlyHint = true };
        }

        return tool;
    }

    private static JsonElement InputSchema(WebMcpToolPlan plan)
    {
        var properties = new JsonObject();

        foreach (var input in plan.Inputs)
        {
            var described = new JsonObject { ["type"] = input.JsonType };
            if (input.Format.Length > 0)
            {
                described["format"] = input.Format;
            }

            properties[input.Name] = described;
        }

        // Path parameters are the one set whose requiredness is structural: the URL cannot be built
        // without them. Everything else is the endpoint's own validation to enforce.
        var required = new JsonArray();
        foreach (var name in plan.PathProperties)
        {
            required.Add(name);
        }

        return JsonSerializer.SerializeToElement<JsonNode>(
            new JsonObject { ["type"] = "object", ["properties"] = properties, ["required"] = required });
    }
}
