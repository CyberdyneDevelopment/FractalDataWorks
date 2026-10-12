using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Everything a transport needs to decide about one tool: what the agent is asked for, and how
/// what it sends becomes an HTTP request.
/// </summary>
/// <param name="Tool">The descriptor this plan was derived from.</param>
/// <param name="Inputs">What the agent is asked for, in declaration order. Never the preset field.</param>
/// <param name="PathProperties">The request properties the route's parameters bind to, in route order. These are the required inputs: the URL cannot be built without them.</param>
/// <param name="RouteParts">The route, as literal text and parameters.</param>
/// <param name="QueryProperties">Inputs sent as the query string. Empty for a tool that carries a body.</param>
/// <param name="HasBody">Whether the call carries a JSON body.</param>
/// <param name="RecoveryProperty">The one parameter a failed call can recover by listing, or <see langword="null"/> when the tool offers no recovery.</param>
/// <remarks>
/// <para>
/// This is why two transports can serve one catalogue without drifting. The rules live here, once:
/// which properties an agent is asked for, which travel in the path, which in the query string and
/// which in the body, and which value the tool fixes itself. <see cref="WebMcpJsGenerator"/>
/// RENDERS a plan as JavaScript for a browser agent; the MCP transport EXECUTES the same plan for a
/// headless one. Neither decides anything the other could decide differently.
/// </para>
/// <para>
/// Derived rather than stored on the descriptor because every part of it follows from the
/// descriptor, and a second copy of a derivable fact is a second place for it to be wrong.
/// </para>
/// </remarks>
public sealed record WebMcpToolPlan(
    WebMcpToolDescriptor Tool,
    IReadOnlyList<WebMcpToolInput> Inputs,
    IReadOnlyList<string> PathProperties,
    IReadOnlyList<WebMcpRoutePart> RouteParts,
    IReadOnlyList<string> QueryProperties,
    bool HasBody,
    string? RecoveryProperty)
{
    /// <summary>Derives the plan for one tool.</summary>
    /// <param name="tool">The tool.</param>
    /// <param name="logger">Receives one entry per request property mapped or skipped.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="InvalidOperationException">
    /// A route parameter binds to no request property, or a preset field is declared without its
    /// value. <see cref="WebMcpToolRegistry"/> refuses both before a tool is ever served, so either
    /// reaching here means a descriptor was built by hand wrongly. Guessing a binding would hand the
    /// agent a tool whose URL cannot be built, which fails as a 404 it cannot interpret.
    /// </exception>
    public static WebMcpToolPlan Create(WebMcpToolDescriptor tool, ILogger logger)
    {
        if (tool.PresetFieldName is not null && tool.PresetValue is null)
        {
            throw new InvalidOperationException(
                $"Tool '{tool.Name}' declares preset field '{tool.PresetFieldName}' with no value. A preset is a "
                + "value the tool fixes for the agent; without one there is nothing to fix it to.");
        }

        var parameters = WebMcpRouteTemplate.ParameterNames(tool.Route);
        var pathProperties = parameters.Select(parameter => BoundProperty(tool, parameter)).ToList();

        // Every request property JSON Schema can describe, computed once. The inputs and the query
        // string are both cut from this one list, so a property cannot be offered to the agent and
        // then left out of the call it makes.
        var described = tool.RequestType is null
            ? []
            : Describe(tool.RequestType, tool.Name, logger);

        var hasBody = NeedsRequestBody(tool.HttpMethod);

        return new WebMcpToolPlan(
            tool,
            // The preset field is never asked of the agent: the tool's own declaration fixes it, the
            // same treatment a path parameter gets.
            tool.PresetFieldName is null
                ? described
                : described.Where(input => !string.Equals(input.Name, tool.PresetFieldName, StringComparison.OrdinalIgnoreCase)).ToList(),
            pathProperties,
            SplitRoute(tool.Route, parameters, pathProperties),
            hasBody || tool.RequestType is null
                ? []
                : described
                    .Select(input => input.Name)
                    .Where(name => !pathProperties.Contains(name, StringComparer.OrdinalIgnoreCase))
                    .ToList(),
            hasBody,
            // A tool with more than one parameter offers no recovery: which collection a second
            // parameter selects from is not derivable from the route, and naming the wrong one is
            // worse than naming none.
            tool.ParentListRoute is not null && pathProperties.Count == 1 ? pathProperties[0] : null);
    }

    /// <summary>Whether a verb carries a JSON body.</summary>
    /// <param name="httpMethod">The verb.</param>
    /// <returns><see langword="true"/> for POST, PUT and PATCH.</returns>
    private static bool NeedsRequestBody(string httpMethod)
        => string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            || string.Equals(httpMethod, "PUT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(httpMethod, "PATCH", StringComparison.OrdinalIgnoreCase);

    /// <summary>The request property a route parameter binds to, in the property's own casing.</summary>
    /// <remarks>
    /// Route parameters bind case-insensitively but an agent's input is read by exact key, so the
    /// schema and the URL must both use the property's own casing or the value silently arrives
    /// undefined.
    /// </remarks>
    private static string BoundProperty(WebMcpToolDescriptor tool, string parameter)
        => tool.RequestType?.GetProperty(parameter, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.Name
           ?? throw new InvalidOperationException(
               $"Tool '{tool.Name}' routes '{tool.Route}', whose parameter '{parameter}' binds to no property of "
               + $"{tool.RequestType?.FullName ?? "a tool with no request type"}. The URL cannot be built.");

    private static List<WebMcpRoutePart> SplitRoute(
        string route,
        IReadOnlyList<string> parameters,
        List<string> pathProperties)
    {
        if (parameters.Count == 0)
        {
            return [new WebMcpRoutePart(route, IsParameter: false)];
        }

        var parts = new List<WebMcpRoutePart>();
        var remaining = route;

        for (var i = 0; i < parameters.Count; i++)
        {
            var open = remaining.IndexOf('{', StringComparison.Ordinal);
            var close = remaining.IndexOf('}', StringComparison.Ordinal);
            if (open < 0 || close < open)
            {
                break;
            }

            parts.Add(new WebMcpRoutePart(remaining[..open], IsParameter: false));
            parts.Add(new WebMcpRoutePart(pathProperties[i], IsParameter: true));
            remaining = remaining[(close + 1)..];
        }

        if (remaining.Length > 0)
        {
            parts.Add(new WebMcpRoutePart(remaining, IsParameter: false));
        }

        return parts;
    }

    private static List<WebMcpToolInput> Describe(Type requestType, string toolName, ILogger logger)
    {
        var inputs = new List<WebMcpToolInput>();

        foreach (var property in requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
            {
                continue;
            }

            var (jsonType, format) = JsonSchemaOf(property.PropertyType);
            if (jsonType.Length == 0)
            {
                WebMcpLog.SchemaPropertySkipped(
                    logger,
                    toolName,
                    property.Name,
                    property.PropertyType.FullName ?? property.PropertyType.Name);
                continue;
            }

            WebMcpLog.SchemaPropertyMapped(logger, toolName, property.Name, jsonType, format);
            inputs.Add(new WebMcpToolInput(property.Name, jsonType, format));
        }

        return inputs;
    }

    private static (string JsonType, string Format) JsonSchemaOf(Type clrType)
    {
        var underlying = Nullable.GetUnderlyingType(clrType);
        if (underlying is not null)
        {
            return JsonSchemaOf(underlying);
        }

        if (clrType == typeof(string)) return ("string", string.Empty);
        if (clrType == typeof(bool)) return ("boolean", string.Empty);
        if (clrType == typeof(int)) return ("integer", string.Empty);
        if (clrType == typeof(long)) return ("integer", string.Empty);
        if (clrType == typeof(short)) return ("integer", string.Empty);
        if (clrType == typeof(double)) return ("number", string.Empty);
        if (clrType == typeof(float)) return ("number", string.Empty);
        if (clrType == typeof(decimal)) return ("number", string.Empty);
        if (clrType == typeof(Guid)) return ("string", "uuid");
        if (clrType == typeof(DateTime)) return ("string", "date-time");
        if (clrType == typeof(DateTimeOffset)) return ("string", "date-time");
        if (clrType == typeof(DateOnly)) return ("string", "date");
        if (clrType == typeof(TimeOnly)) return ("string", "time");

        // Arrays, collections and nested objects have no primitive schema. Skipped and logged,
        // never guessed at.
        return (string.Empty, string.Empty);
    }
}
