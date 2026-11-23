using System;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// What the builder of the Ask component needs from the host (FDW-6142): the answer requests it keeps, the answer policies it judges a call under and the pipelines it reads the continuation from. Each is
/// read when the step runs, from the scope of the run: answering starts runs, so what keeps the requests depends on the run contract, which the graph pipeline factory is built before.
/// </summary>
public sealed class FlowAskServices
{
    private readonly Func<IAnswerRequests> _requests;
    private readonly Func<IAnswerPolicies> _policies;
    private readonly Func<IPipelineConfigurationProvider> _pipelines;

    /// <summary>Initializes a new instance of the <see cref="FlowAskServices"/> class.</summary>
    /// <param name="requests">Gives what keeps the answer requests.</param>
    /// <param name="policies">Gives what judges a call under an answer policy.</param>
    /// <param name="pipelines">Gives what reads the continuation pipeline.</param>
    public FlowAskServices(Func<IAnswerRequests> requests, Func<IAnswerPolicies> policies, Func<IPipelineConfigurationProvider> pipelines)
    {
        _requests = requests;
        _policies = policies;
        _pipelines = pipelines;
    }

    /// <summary>Gets what keeps the answer requests.</summary>
    public IAnswerRequests Requests => _requests();

    /// <summary>Gets what judges a call under an answer policy.</summary>
    public IAnswerPolicies Policies => _policies();

    /// <summary>Gets what reads the continuation pipeline.</summary>
    public IPipelineConfigurationProvider Pipelines => _pipelines();
}
