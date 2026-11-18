using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The base of every data option rule route (FDW-6339, stage 1: FDW-6348). Each route's base declares its verb, path, policy and shapes;
/// until stage 2 builds a route it answers <c>RouteNotBuilt</c> (404, its own code), never a made-up success, so a caller can code and test
/// against the true shapes now. Stage 2 replaces <see cref="HandleAsync"/> route by route and keeps every shape.
/// </summary>
/// <typeparam name="TRequest">The route's request.</typeparam>
/// <typeparam name="TResponse">The route's answer.</typeparam>
public abstract class DataOptionRuleContractEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionRuleContractEndpointBase{TRequest, TResponse}"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected DataOptionRuleContractEndpointBase(ILogger? logger)
    {
        EndpointLogger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the logger of the concrete endpoint.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Gets the route's name, as the not-built answer and the log name it.</summary>
    protected abstract string RouteName { get; }

    /// <inheritdoc />
    public override Task HandleAsync(TRequest req, CancellationToken ct)
        => Send.ResultAsync(ResultHttpStatusMapper.Answer(NotBuilt(), HttpContext));

    /// <summary>The answer of a route stage 2 has not built: <c>RouteNotBuilt</c>, naming the route.</summary>
    /// <returns>The failed result.</returns>
    protected IGenericResult NotBuilt() => GenericResult.Failure(DataOptionRulesEndpointLog.RouteNotBuilt(EndpointLogger, RouteName));

    /// <summary>Lets a host's closure add its own configuration, such as its documentation tags.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }
}
