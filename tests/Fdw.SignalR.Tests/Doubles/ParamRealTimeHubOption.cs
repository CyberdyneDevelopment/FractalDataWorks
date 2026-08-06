using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.SignalR.Tests.Doubles;

/// <summary>
/// Parameterized <see cref="RealTimeHubOptionBase"/> used to exercise the base constructor's
/// null-argument guards for <c>route</c> and <c>hubType</c>.
/// </summary>
public sealed class ParamRealTimeHubOption : RealTimeHubOptionBase
{
    /// <summary>Initializes a new instance with caller-supplied route and hub type.</summary>
    public ParamRealTimeHubOption(string route, Type hubType)
        : base(1, "Param", route, hubType, authorizationPolicy: null)
    {
    }

    /// <inheritdoc/>
    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
    {
    }

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints)
    {
    }
}
