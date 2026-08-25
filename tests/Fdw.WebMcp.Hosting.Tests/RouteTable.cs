using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>Builds an <see cref="EndpointDataSource"/> standing in for the application's routes.</summary>
internal sealed class RouteTable : EndpointDataSource
{
    private readonly List<Endpoint> _endpoints = [];

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);

    /// <summary>Adds one route, as the router would hold it.</summary>
    public RouteTable Add(string route, Type endpointType, Type? reqDto = null, Type? resDto = null, params string[] verbs)
    {
        _endpoints.Add(new RouteEndpoint(
            requestDelegate: static _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse(route),
            order: 0,
            metadata: new EndpointMetadataCollection(
                new EndpointDefinition { EndpointType = endpointType, ReqDtoType = reqDto, ResDtoType = resDto },
                new HttpMethodMetadata(verbs)),
            displayName: endpointType.Name));

        return this;
    }

    /// <summary>Adds a route carrying no FastEndpoints metadata, as a plain minimal-API route would.</summary>
    public RouteTable AddUndefined(string route)
    {
        _endpoints.Add(new RouteEndpoint(
            requestDelegate: static _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse(route),
            order: 0,
            metadata: new EndpointMetadataCollection(new HttpMethodMetadata(["GET"])),
            displayName: route));

        return this;
    }
}
