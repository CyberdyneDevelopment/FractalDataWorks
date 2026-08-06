using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using Fdw.SignalR.Tests.Doubles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Fdw.SignalR.Tests;

/// <summary>
/// Tests for <see cref="RealTimeHubEndpointExtensions.MapRealTimeHubs"/> and the
/// authorization-policy branch of <see cref="RealTimeHubOptionBase"/>'s mapping helper.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class RealTimeHubEndpointExtensionsTests
{
    [Fact]
    public void MapRealTimeHubsWithNullEndpointsThrows()
    {
        Should.Throw<ArgumentNullException>(() => RealTimeHubEndpointExtensions.MapRealTimeHubs(null!));
    }

    [Fact]
    public void MapRealTimeHubsMapsEveryHubRoute()
    {
        var builder = WebApplication.CreateBuilder();
        RealTimeHubs.Register(builder);
        var app = builder.Build();

        app.MapRealTimeHubs();

        var patterns = RoutePatterns(app);
        patterns.ShouldContain(p => p.Contains("/hubs/pipelines", StringComparison.Ordinal));
        patterns.ShouldContain(p => p.Contains("/hubs/calculations", StringComparison.Ordinal));
        patterns.ShouldContain(p => p.Contains("/hubs/schema-discovery", StringComparison.Ordinal));
        patterns.ShouldContain(p => p.Contains("/hubs/messages", StringComparison.Ordinal));
    }

    [Fact]
    public void MapRealTimeHubsWithLoggerFactoryStillMapsRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        RealTimeHubs.Register(builder);
        var app = builder.Build();

        // Passing a non-null logger factory exercises the mapping-logging branch.
        app.MapRealTimeHubs(NullLoggerFactory.Instance);

        RoutePatterns(app).ShouldContain(p => p.Contains("/hubs/pipelines", StringComparison.Ordinal));
    }

    [Fact]
    public void MapRealTimeHubsRequiresAuthorizationOnEveryHub()
    {
        // Why: authentication is mandatory (FDW-545). Every hub endpoint — including those whose
        // option declares no explicit policy (null → the default RequireAuthorization() branch of
        // MapHubAt) — must carry authorization metadata. No FDW real-time hub is mapped anonymously.
        var builder = WebApplication.CreateBuilder();
        RealTimeHubs.Register(builder);
        var app = builder.Build();

        app.MapRealTimeHubs();

        var hubEndpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => (e.RoutePattern.RawText ?? string.Empty).Contains("/hubs/", StringComparison.Ordinal))
            .ToList();

        hubEndpoints.ShouldNotBeEmpty();
        hubEndpoints.ShouldAllBe(e => e.Metadata.GetMetadata<IAuthorizeData>() != null);
    }

    [Fact]
    public void OptionMapAppliesAuthorizationPolicyWhenDeclared()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddAuthorization();
        var app = builder.Build();
        var option = new TestRealTimeHubOption();

        option.Map(app);

        var authEndpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .First(e => (e.RoutePattern.RawText ?? string.Empty).Contains("/hubs/test", StringComparison.Ordinal));

        authEndpoint.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
    }

    private static System.Collections.Generic.List<string> RoutePatterns(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .ToList();
}
