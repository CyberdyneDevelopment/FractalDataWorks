using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>The components of a run in flight changed.</summary>
[TypeOption(typeof(PipelineEventKinds), "ComponentsChanged")]
public sealed class ComponentsChangedPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ComponentsChangedPipelineEventKind"/> class.</summary>
    public ComponentsChangedPipelineEventKind() : base("ComponentsChanged", "The components of a run in flight changed.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.Components is { } update)
        {
            await broadcaster.BroadcastComponents(update, request.OrgId).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
