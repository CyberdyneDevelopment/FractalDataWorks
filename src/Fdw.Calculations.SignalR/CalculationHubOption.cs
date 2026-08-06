using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Calculations.Contracts.Hubs;
using Fdw.Collections.Attributes;
using Fdw.SignalR;

namespace Fdw.Calculations.SignalR;

/// <summary>
/// Registers the calculation hub against the <see cref="RealTimeHubs"/> collection.
/// </summary>
/// <remarks>
/// Declares the route and broadcaster wiring for <see cref="CalculationHub"/>. The hub authorizes
/// via its own <c>[Authorize]</c> attribute, so no mapping-level policy is declared here.
/// </remarks>
[TypeOption(typeof(RealTimeHubs), "Calculation")]
public sealed class CalculationHubOption : RealTimeHubOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationHubOption"/> class.
    /// </summary>
    public CalculationHubOption()
        : base(2, "Calculation", "/hubs/calculations", typeof(CalculationHub), authorizationPolicy: null)
    {
    }

    /// <inheritdoc/>
    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
        => services.AddBroadcaster<
            ICalculationNotifier,
            CalculationNotifier,
            CalculationHub,
            ICalculationHubClient>(loggerFactory);

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints) => MapHubAt<CalculationHub>(endpoints);
}
