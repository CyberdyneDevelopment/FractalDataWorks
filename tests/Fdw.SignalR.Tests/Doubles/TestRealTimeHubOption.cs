using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.SignalR.Tests.Doubles;

/// <summary>
/// Concrete <see cref="RealTimeHubOptionBase"/> for exercising the option base directly, including
/// the policy-applying branch of <c>MapHubAt</c> (constructed with a non-null authorization policy).
/// </summary>
public sealed class TestRealTimeHubOption : RealTimeHubOptionBase
{
    /// <summary>Records whether <see cref="RegisterServices"/> ran.</summary>
    public bool RegisterServicesCalled { get; private set; }

    /// <summary>Records whether <see cref="Map"/> ran.</summary>
    public bool MapCalled { get; private set; }

    /// <summary>Initializes a new instance of the <see cref="TestRealTimeHubOption"/> class.</summary>
    public TestRealTimeHubOption()
        : base(99, "Test", "/hubs/test", typeof(TestHub), authorizationPolicy: "test-policy")
    {
    }

    /// <inheritdoc/>
    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
        => RegisterServicesCalled = true;

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints)
    {
        MapCalled = true;
        MapHubAt<TestHub>(endpoints);
    }
}
