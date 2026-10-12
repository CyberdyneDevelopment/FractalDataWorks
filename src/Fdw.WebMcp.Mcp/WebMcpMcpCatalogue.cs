using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.WebMcp.Hosting;
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
