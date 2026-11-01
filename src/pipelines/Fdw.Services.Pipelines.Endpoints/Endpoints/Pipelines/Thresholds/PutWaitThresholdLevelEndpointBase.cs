using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// <c>PUT /etl/wait-thresholds/...</c>: states the level of thresholds on waiting runs an owner states (FDW-3161), its mode and its thresholds together. A level that cannot be
/// (see <c>WaitThresholdValidator</c>), or that names a notification rule or a pipeline that does not exist, is a 400 naming the member; a pipeline or schedule that does not exist
/// is a 404. Needs <c>pipelines:write</c>.
/// </summary>
public abstract class PutWaitThresholdLevelEndpointBase : WaitThresholdLevelEndpointBase<WaitThresholdLevelPayload>
{
    /// <summary>Initializes a new instance of the <see cref="PutWaitThresholdLevelEndpointBase"/> class.</summary>
    /// <param name="store">Writes the level.</param>
    protected PutWaitThresholdLevelEndpointBase(IWaitThresholdStore store) : base(store)
    {
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Put(RoutePath);
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "State the thresholds on waiting runs an owner states";
            s.Description = "Replaces the mode (Inherit, Own or None) and the thresholds the server, a pipeline or a schedule states, together. Own carries at least one threshold; Inherit and None carry none; the server takes Own or None.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(WaitThresholdLevelPayload request, CancellationToken ct) =>
        await Answer(await Store.Write(OwnerKind, OwnerName(), request, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
}
