using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.WebMcp.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Joins the endpoints that declared themselves WebMCP tools against the routes the application
/// actually serves, producing the descriptors served at <c>/.well-known/webmcp.js</c>.
/// </summary>
/// <remarks>
/// Tools are not searched for. They arrive already declared, in <see cref="DeclaredWebMcpTools"/>,
/// put there by the endpoint option that switched the endpoint on. All this type adds is the half of
/// a tool the declaration deliberately does not carry: the route and the verb, read from the live
/// route table so they are by construction the ones the router will match.
///
/// Reading them from the router rather than from the endpoint is the whole point. A route declared a
/// second time on an attribute is free to drift from the FastEndpoints <c>Configure()</c> body that
/// defines it, and an agent handed a drifted route gets a 404 it cannot interpret.
/// </remarks>
internal sealed class WebMcpToolRegistry : IWebMcpToolRegistry
{
    private readonly List<WebMcpToolDescriptor> _tools = [];

    /// <inheritdoc/>
    public IReadOnlyList<WebMcpToolDescriptor> Tools => _tools;

    /// <summary>
    /// Resolves declared tools against the application's route table.
    /// </summary>
    /// <param name="declarations">What the endpoint options declared.</param>
    /// <param name="endpoints">The application's endpoints, after routing is built.</param>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// The declarations are passed in rather than read from <see cref="DeclaredWebMcpTools"/> here.
    /// That collection is process-wide, so a join that reached for it directly could only ever be
    /// exercised against whatever the whole process had declared.
    /// </remarks>
    internal void Resolve(
        IReadOnlyList<WebMcpToolDeclaration> declarations,
        IEnumerable<Endpoint> endpoints,
        ILogger logger)
    {
        var routesByEndpointType = MapRoutesByEndpointType(endpoints);

        foreach (var declaration in declarations)
        {
            var typeName = declaration.EndpointType.FullName ?? declaration.EndpointType.Name;
            WebMcpLog.DiscoveringTool(logger, typeName);

            if (!routesByEndpointType.TryGetValue(declaration.EndpointType, out var candidates))
            {
                WebMcpLog.ToolSkipped(logger, typeName);
                continue;
            }

            var selected = Select(candidates, declaration.HttpMethodOverride);
            if (selected is null)
            {
                WebMcpLog.ToolRouteAmbiguous(logger, typeName, candidates.Count);
                continue;
            }

            WebMcpLog.RouteResolved(logger, typeName, selected.Value.Route, "application route table");
            WebMcpLog.HttpMethodResolved(
                logger,
                typeName,
                selected.Value.HttpMethod,
                declaration.HttpMethodOverride is null ? "application route table" : "WebMcpTool.HttpMethod");
            WebMcpLog.EndpointTypesResolved(
                logger,
                typeName,
                selected.Value.RequestType?.FullName ?? "none",
                selected.Value.ResponseType?.FullName ?? "none");

            var unbindable = FirstUnbindableParameter(selected.Value.Route, selected.Value.RequestType);
            if (unbindable is not null)
            {
                WebMcpLog.ToolParameterUnbindable(logger, typeName, selected.Value.Route, unbindable);
                continue;
            }

            _tools.Add(new WebMcpToolDescriptor(
                declaration.Name,
                declaration.Description,
                selected.Value.Route,
                selected.Value.HttpMethod,
                declaration.ReadOnly,
                selected.Value.RequestType,
                selected.Value.ResponseType));

            WebMcpLog.ToolDiscovered(logger, declaration.Name, selected.Value.Route, selected.Value.HttpMethod);
        }

        AttachParentLists(logger);

        WebMcpLog.ToolsRegistered(logger, _tools.Count);
    }

    /// <summary>
    /// Names the first route parameter the request type cannot supply, if any.
    /// </summary>
    /// <param name="route">The resolved route template.</param>
    /// <param name="requestType">The endpoint's request DTO, or <see langword="null"/> when it has none.</param>
    /// <returns>The offending parameter name, or <see langword="null"/> when every parameter binds.</returns>
    /// <remarks>
    /// A tool whose URL cannot be built is worse than a missing one: the agent gets a 404 it cannot
    /// distinguish from a genuine empty result, and no amount of retrying will fix it. Refusing it
    /// here is the same call the registry already makes for a route it cannot resolve at all.
    /// </remarks>
    private static string? FirstUnbindableParameter(string route, Type? requestType)
    {
        var parameters = WebMcpRouteTemplate.ParameterNames(route);
        if (parameters.Count == 0)
        {
            return null;
        }

        if (requestType is null)
        {
            return parameters[0];
        }

        foreach (var parameter in parameters)
        {
            if (requestType.GetProperty(
                    parameter,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is null)
            {
                return parameter;
            }
        }

        return null;
    }

    /// <summary>
    /// Points each parameterised tool at the tool that lists its valid values.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// Runs as a second pass because the parent is itself a tool, and the first pass is what decides
    /// which declarations became tools at all.
    ///
    /// The parent is the collection the parameter selects from: for <c>/connections/{Name}/health</c>
    /// that is the GET tool on <c>/connections</c>. Matched against the RESOLVED routes rather than
    /// by guessing at names, because a resource's list route is frequently computed rather than
    /// written — CrudListEndpointBase builds <c>/{ResourceName}</c> — so nothing textual is reliable.
    ///
    /// A tool with more than one parameter is left alone. Which collection a second parameter selects
    /// from is not derivable from the route, and naming the wrong one is worse than naming none.
    /// </remarks>
    private void AttachParentLists(ILogger logger)
    {
        var listToolsByRoute = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tool in _tools)
        {
            if (WebMcpRouteTemplate.ParameterNames(tool.Route).Count == 0
                && string.Equals(tool.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                listToolsByRoute[tool.Route] = tool.Name;
            }
        }

        for (var i = 0; i < _tools.Count; i++)
        {
            var parameters = WebMcpRouteTemplate.ParameterNames(_tools[i].Route);
            if (parameters.Count != 1)
            {
                continue;
            }

            var prefix = _tools[i].Route[.._tools[i].Route.IndexOf('{', StringComparison.Ordinal)]
                .TrimEnd('/');

            if (prefix.Length == 0 || !listToolsByRoute.TryGetValue(prefix, out var parentToolName))
            {
                continue;
            }

            WebMcpLog.ParentListResolved(logger, _tools[i].Name, parameters[0], parentToolName, prefix);

            _tools[i] = _tools[i] with
            {
                ParentListRoute = prefix,
                ParentListToolName = parentToolName,
            };
        }
    }

    /// <summary>One route the application serves for a given endpoint class.</summary>
    private readonly record struct RouteCandidate(
        string Route,
        string HttpMethod,
        Type? RequestType,
        Type? ResponseType);

    /// <summary>
    /// Narrows an endpoint's routes to the single one a tool should call.
    /// </summary>
    /// <returns>The chosen route, or <see langword="null"/> when the choice is ambiguous.</returns>
    private static RouteCandidate? Select(List<RouteCandidate> candidates, string? httpMethodOverride)
    {
        var considered = httpMethodOverride is null
            ? candidates
            : [.. candidates.Where(c => string.Equals(c.HttpMethod, httpMethodOverride, StringComparison.OrdinalIgnoreCase))];

        return considered.Count == 1 ? considered[0] : null;
    }

    /// <summary>
    /// Indexes the application's route table by the endpoint class each route was built from.
    /// </summary>
    /// <remarks>
    /// The endpoint class is read off FastEndpoints' <c>EndpointDefinition</c> metadata by NAME
    /// rather than by type, so this package keeps no hard dependency on a particular FastEndpoints
    /// version — the same stance the rest of this package takes toward it.
    /// </remarks>
    private static Dictionary<Type, List<RouteCandidate>> MapRoutesByEndpointType(IEnumerable<Endpoint> endpoints)
    {
        var map = new Dictionary<Type, List<RouteCandidate>>();

        foreach (var endpoint in endpoints)
        {
            if (endpoint is not RouteEndpoint routeEndpoint)
            {
                continue;
            }

            var definition = routeEndpoint.Metadata.FirstOrDefault(
                m => m is not null && string.Equals(m.GetType().Name, "EndpointDefinition", StringComparison.Ordinal));

            if (definition is null)
            {
                continue;
            }

            if (ReadTypeProperty(definition, "EndpointType") is not { } endpointType)
            {
                continue;
            }

            if (routeEndpoint.RoutePattern.RawText is not { } rawRoute || string.IsNullOrWhiteSpace(rawRoute))
            {
                continue;
            }

            var route = rawRoute.StartsWith('/') ? rawRoute : "/" + rawRoute;

            foreach (var httpMethod in routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
            {
                if (!map.TryGetValue(endpointType, out var list))
                {
                    list = [];
                    map[endpointType] = list;
                }

                list.Add(new RouteCandidate(
                    route,
                    httpMethod,
                    ReadTypeProperty(definition, "ReqDtoType"),
                    ReadTypeProperty(definition, "ResDtoType")));
            }
        }

        return map;
    }

    private static Type? ReadTypeProperty(object definition, string propertyName)
        => definition.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(definition) as Type;
}
