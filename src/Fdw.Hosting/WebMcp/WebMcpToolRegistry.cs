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

            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                var toolAttr = type.GetCustomAttribute<WebMcpToolAttribute>();
                if (toolAttr is null)
                {
                    continue;
                }

                WebMcpLog.DiscoveringTool(logger, type.FullName ?? type.Name);

                var route = ResolveRoute(type);
                if (route is null)
                {
                    WebMcpLog.ToolSkipped(logger, type.FullName ?? type.Name);
                    continue;
                }

                var httpMethod = ResolveHttpMethod(type, toolAttr);
                var (requestType, responseType) = ResolveTypeArguments(type);

                var descriptor = new WebMcpToolDescriptor(
                    toolAttr.Name,
                    toolAttr.Description,
                    route,
                    httpMethod,
                    toolAttr.ReadOnly,
                    requestType,
                    responseType);

                _tools.Add(descriptor);
                WebMcpLog.ToolDiscovered(logger, toolAttr.Name, route, httpMethod);
            }
        }

        WebMcpLog.ToolsRegistered(logger, _tools.Count);
    }

    // ── Route resolution ────────────────────────────────────────────────────

    private static string? ResolveRoute(Type endpointType)
    {
        // Strategy 1: look for a public const or static string field named "Route"
        var routeField = endpointType.GetField(
            "Route",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (routeField is not null && routeField.FieldType == typeof(string))
        {
            var value = routeField.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        // Strategy 2: look for a public static string property named "Route"
        var routeProp = endpointType.GetProperty(
            "Route",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (routeProp is not null && routeProp.PropertyType == typeof(string))
        {
            var value = routeProp.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        // Strategy 3: look for a FastEndpoints [Http*] attribute by name
        //   (avoids a hard dependency on FastEndpoints from this project)
        foreach (var attr in endpointType.GetCustomAttributes())
        {
            var attrType = attr.GetType();
            var attrName = attrType.Name;

            if (!string.Equals(attrName, "HttpGetAttribute", StringComparison.Ordinal)
                && !string.Equals(attrName, "HttpPostAttribute", StringComparison.Ordinal)
                && !string.Equals(attrName, "HttpPutAttribute", StringComparison.Ordinal)
                && !string.Equals(attrName, "HttpPatchAttribute", StringComparison.Ordinal)
                && !string.Equals(attrName, "HttpDeleteAttribute", StringComparison.Ordinal))
            {
                continue;
            }

            // FastEndpoints Http* attributes expose a "Route" property
            var routeAttrProp = attrType.GetProperty("Route");
            if (routeAttrProp is not null)
            {
                var value = routeAttrProp.GetValue(attr) as string;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    // ── HTTP method resolution ───────────────────────────────────────────────

    private static string ResolveHttpMethod(Type endpointType, WebMcpToolAttribute toolAttr)
    {
        // Explicit override wins
        if (!string.IsNullOrWhiteSpace(toolAttr.HttpMethod))
        {
            return toolAttr.HttpMethod!.ToUpperInvariant();
        }

        // Inspect FastEndpoints [Http*] attribute names
        foreach (var attr in endpointType.GetCustomAttributes())
        {
            var name = attr.GetType().Name;

            if (string.Equals(name, "HttpGetAttribute", StringComparison.Ordinal)) return "GET";
            if (string.Equals(name, "HttpPostAttribute", StringComparison.Ordinal)) return "POST";
            if (string.Equals(name, "HttpPutAttribute", StringComparison.Ordinal)) return "PUT";
            if (string.Equals(name, "HttpPatchAttribute", StringComparison.Ordinal)) return "PATCH";
            if (string.Equals(name, "HttpDeleteAttribute", StringComparison.Ordinal)) return "DELETE";
        }

        // Fall back to base class name heuristics
        var typeName = endpointType.Name;
        if (typeName.StartsWith("Create", StringComparison.OrdinalIgnoreCase)) return "POST";
        if (typeName.StartsWith("Update", StringComparison.OrdinalIgnoreCase)) return "PUT";
        if (typeName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)) return "DELETE";

        return "GET";
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
