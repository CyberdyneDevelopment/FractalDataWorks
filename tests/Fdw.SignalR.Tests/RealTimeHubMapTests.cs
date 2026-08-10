using System;
using System.Linq;
using Fdw.SignalR.Tests.Doubles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.SignalR.Tests;

/// <summary>
/// Tests that a hub option maps itself with the authorization it declares.
/// </summary>
/// <remarks>
/// This file used to also cover MapRealTimeHubs — the sweep that called every hub's Map. That sweep
/// now runs inside the host's Initialize phase, where exercising it means standing up a host rather
/// than calling one method, so those four tests went with the extension. What survives is the part
/// that is still a unit: an option, asked to map itself, carries its policy onto the route.
/// </remarks>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class RealTimeHubMapTests
{
    /// <summary>
    /// A hub declaring an authorization policy produces a route carrying that requirement.
    /// </summary>
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
}
