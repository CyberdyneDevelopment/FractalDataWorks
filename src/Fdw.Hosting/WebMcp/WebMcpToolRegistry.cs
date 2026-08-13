using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.WebMcp;

/// <summary>
/// Scans assemblies for endpoint classes decorated with <see cref="WebMcpToolAttribute"/>
/// and builds the set of WebMCP tool descriptors served at <c>/.well-known/webmcp.js</c>.
/// </summary>
internal sealed class WebMcpToolRegistry : IWebMcpToolRegistry
{
    private readonly List<WebMcpToolDescriptor> _tools = [];

    /// <inheritdoc/>
    public IReadOnlyList<WebMcpToolDescriptor> Tools => _tools;

    /// <summary>
    /// Scans the provided assemblies and populates the tool registry.
    /// Skips any type where a route cannot be determined, logging a warning.
    /// </summary>
    internal void Discover(IEnumerable<Assembly> assemblies, ILogger logger)
    {
        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Partial load — iterate what succeeded
                types = ex.Types is not null
                    ? [.. Array.FindAll(ex.Types, t => t is not null)!]
                    : [];
            }

            WebMcpLog.AssemblyScanned(logger, assembly.GetName().Name ?? assembly.FullName ?? "unknown", types.Length);

            foreach (var type in types)
            {
                var descriptor = DescribeTool(type, logger);
                if (descriptor is null)
                {
                    continue;
                }

                _tools.Add(descriptor);
                WebMcpLog.ToolDiscovered(logger, descriptor.Name, descriptor.Route, descriptor.HttpMethod);
            }
        }

        WebMcpLog.ToolsRegistered(logger, _tools.Count);
    }

    /// <summary>
    /// Builds the descriptor for one candidate type, or returns <see langword="null"/> when the type
    /// is not a tool or its route cannot be resolved.
    /// </summary>
    private static WebMcpToolDescriptor? DescribeTool(Type type, ILogger logger)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return null;
        }

        var toolAttr = type.GetCustomAttribute<WebMcpToolAttribute>();
        if (toolAttr is null)
        {
            return null;
        }

        WebMcpLog.DiscoveringTool(logger, type.FullName ?? type.Name);

        var route = ResolveRoute(type, logger);
        if (route is null)
        {
            WebMcpLog.ToolSkipped(logger, type.FullName ?? type.Name);
            return null;
        }

        var (requestType, responseType) = ResolveTypeArguments(type);

        WebMcpLog.EndpointTypesResolved(
            logger,
            type.FullName ?? type.Name,
            requestType?.FullName ?? "none",
            responseType?.FullName ?? "none");

        return new WebMcpToolDescriptor(
            toolAttr.Name,
            toolAttr.Description,
            route,
            ResolveHttpMethod(type, toolAttr, logger),
            toolAttr.ReadOnly,
            requestType,
            responseType);
    }

    // ── Route resolution ────────────────────────────────────────────────────

    /// <summary>
    /// Maps a FastEndpoints attribute name to its HTTP verb. Keyed by name rather than type so this
    /// project keeps no hard dependency on a particular FastEndpoints version.
    /// </summary>
    private static readonly Dictionary<string, string> HttpAttributeMethods = new(StringComparer.Ordinal)
    {
        ["HttpGetAttribute"] = "GET",
        ["HttpPostAttribute"] = "POST",
        ["HttpPutAttribute"] = "PUT",
        ["HttpPatchAttribute"] = "PATCH",
        ["HttpDeleteAttribute"] = "DELETE",
    };

    private static string? ResolveRoute(Type endpointType, ILogger logger)
    {
        var typeName = endpointType.FullName ?? endpointType.Name;

        if (RouteFromField(endpointType) is { } fieldRoute)
        {
            WebMcpLog.RouteResolved(logger, typeName, fieldRoute, "static Route field");
            return fieldRoute;
        }

        if (RouteFromProperty(endpointType) is { } propertyRoute)
        {
            WebMcpLog.RouteResolved(logger, typeName, propertyRoute, "static Route property");
            return propertyRoute;
        }

        return RouteFromHttpAttribute(endpointType, logger);
    }

    /// <summary>Strategy 1: a public const or static string field named <c>Route</c>.</summary>
    private static string? RouteFromField(Type endpointType)
    {
        var routeField = endpointType.GetField(
            "Route",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (routeField is null || routeField.FieldType != typeof(string))
        {
            return null;
        }

        return routeField.GetValue(null) as string is { } value && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    /// <summary>Strategy 2: a public static string property named <c>Route</c>.</summary>
    private static string? RouteFromProperty(Type endpointType)
    {
        var routeProp = endpointType.GetProperty(
            "Route",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (routeProp is null || routeProp.PropertyType != typeof(string))
        {
            return null;
        }

        return routeProp.GetValue(null) as string is { } value && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    /// <summary>
    /// Strategy 3: a FastEndpoints <c>[Http*]</c> attribute, matched by attribute name so this
    /// project keeps no hard dependency on a particular FastEndpoints version.
    /// </summary>
    private static string? RouteFromHttpAttribute(Type endpointType, ILogger logger)
    {
        foreach (var attr in endpointType.GetCustomAttributes())
        {
            var attrType = attr.GetType();

            if (!HttpAttributeMethods.ContainsKey(attrType.Name))
            {
                continue;
            }

            if (attrType.GetProperty("Route")?.GetValue(attr) as string is { } value
                && !string.IsNullOrWhiteSpace(value))
            {
                WebMcpLog.RouteResolved(logger, endpointType.FullName ?? endpointType.Name, value, attrType.Name);
                return value;
            }
        }

        return null;
    }

    // ── HTTP method resolution ───────────────────────────────────────────────

    private static string ResolveHttpMethod(Type endpointType, WebMcpToolAttribute toolAttr, ILogger logger)
    {
        var typeName = endpointType.FullName ?? endpointType.Name;

        // Explicit override wins
        if (!string.IsNullOrWhiteSpace(toolAttr.HttpMethod))
        {
            var explicitMethod = toolAttr.HttpMethod!.ToUpperInvariant();
            WebMcpLog.HttpMethodResolved(logger, typeName, explicitMethod, "WebMcpTool.HttpMethod");
            return explicitMethod;
        }

        // Inspect FastEndpoints [Http*] attribute names
        foreach (var attr in endpointType.GetCustomAttributes())
        {
            var attrName = attr.GetType().Name;

            if (HttpAttributeMethods.TryGetValue(attrName, out var attributeMethod))
            {
                return Resolved(attributeMethod, attrName);
            }
        }

        // Fall back to base class name heuristics
        if (endpointType.Name.StartsWith("Create", StringComparison.OrdinalIgnoreCase)) return Resolved("POST", "class-name heuristic");
        if (endpointType.Name.StartsWith("Update", StringComparison.OrdinalIgnoreCase)) return Resolved("PUT", "class-name heuristic");
        if (endpointType.Name.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)) return Resolved("DELETE", "class-name heuristic");

        // Why this is logged with its strategy named rather than returned quietly: GET here is a
        // guess, not a declaration. Naming the strategy is what lets a wrong verb be spotted in a
        // log instead of as a failing agent call.
        return Resolved("GET", "default - no method declared");

        string Resolved(string method, string strategy)
        {
            WebMcpLog.HttpMethodResolved(logger, typeName, method, strategy);
            return method;
        }
    }

    // ── Generic type argument resolution ────────────────────────────────────

    private static (Type? RequestType, Type? ResponseType) ResolveTypeArguments(Type endpointType)
    {
        // Walk the inheritance chain looking for a closed generic base such as
        //   Endpoint<TRequest, TResponse>
        //   Endpoint<TRequest>
        //   EndpointWithoutRequest<TResponse>
        var current = endpointType.BaseType;

        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType)
            {
                var args = current.GetGenericArguments();
                var baseName = current.GetGenericTypeDefinition().Name;

                // "Endpoint`2" pattern: TRequest, TResponse
                if (args.Length == 2
                    && baseName.StartsWith("Endpoint", StringComparison.Ordinal))
                {
                    return (args[0], args[1]);
                }

                // "Endpoint`1" pattern: could be TRequest only
                if (args.Length == 1
                    && baseName.StartsWith("Endpoint", StringComparison.Ordinal))
                {
                    // EndpointWithoutRequest<TResponse> has no request type
                    if (baseName.Contains("WithoutRequest", StringComparison.Ordinal))
                    {
                        return (null, args[0]);
                    }

                    return (args[0], null);
                }
            }

            current = current.BaseType;
        }

        return (null, null);
    }
}
