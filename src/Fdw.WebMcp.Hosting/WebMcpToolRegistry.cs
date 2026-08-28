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
    /// <param name="endpointDataSource">The application's endpoints, after routing is built.</param>
    /// <param name="logger">The logger.</param>
    /// <remarks>
    /// The declarations are passed in rather than read from <see cref="DeclaredWebMcpTools"/> here.
    /// That collection is process-wide, so a join that reached for it directly could only ever be
    /// exercised against whatever the whole process had declared.
    /// </remarks>
    internal void Resolve(
        IReadOnlyList<WebMcpToolDeclaration> declarations,
        EndpointDataSource endpointDataSource,
        ILogger logger)
    {
        var routesByEndpointType = MapRoutesByEndpointType(endpointDataSource);

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

        WebMcpLog.ToolsRegistered(logger, _tools.Count);
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
    private static Dictionary<Type, List<RouteCandidate>> MapRoutesByEndpointType(EndpointDataSource endpointDataSource)
    {
        var map = new Dictionary<Type, List<RouteCandidate>>();

        foreach (var endpoint in endpointDataSource.Endpoints)
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
