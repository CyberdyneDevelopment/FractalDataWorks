using System;
using Fdw.SignalR.Tests.Doubles;
using Xunit;

namespace Fdw.SignalR.Tests;

/// <summary>
/// Tests for <see cref="RealTimeHubOptionBase"/> — the hub descriptor base.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class RealTimeHubOptionBaseTests
{
    [Fact]
    public void ConstructorWithNullRouteThrows()
    {
        Should.Throw<ArgumentNullException>(() => new ParamRealTimeHubOption(null!, typeof(TestHub)));
    }

    [Fact]
    public void ConstructorWithNullHubTypeThrows()
    {
        Should.Throw<ArgumentNullException>(() => new ParamRealTimeHubOption("/hubs/x", null!));
    }

    [Fact]
    public void ConstructorExposesRouteHubTypeAndPolicy()
    {
        var option = new TestRealTimeHubOption();

        option.Route.ShouldBe("/hubs/test");
        option.HubType.ShouldBe(typeof(TestHub));
        option.AuthorizationPolicy.ShouldBe("test-policy");
        option.Name.ShouldBe("Test");
    }
}
