using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// <c>GET /etl/wait-thresholds/...</c>: the level of thresholds on waiting runs an owner states (FDW-3161). An owner that states none answers the mode Inherit with no thresholds;
/// a pipeline or schedule that does not exist is a 404. Needs <c>pipelines:read</c>.
/// </summary>
public abstract class GetWaitThresholdLevelEndpointBase : WaitThresholdLevelEndpointBase<EmptyRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetWaitThresholdLevelEndpointBase"/> class.</summary>
    /// <param name="store">Reads the level.</param>
    protected GetWaitThresholdLevelEndpointBase(IWaitThresholdStore store) : base(store)
    {
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(RoutePath);
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Read the level of thresholds on waiting runs an owner states";
            s.Description = "Answers the mode (Inherit, Own or None) the server, a pipeline or a schedule states. The thresholds are attached to the owner through the threshold routes. An owner that states nothing answers Inherit.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(EmptyRequest request, CancellationToken ct) =>
        await Answer(await Store.Read(OwnerKind, OwnerName(), ct).ConfigureAwait(false), ct).ConfigureAwait(false);
}
