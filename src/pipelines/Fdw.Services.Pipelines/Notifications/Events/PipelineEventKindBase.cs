using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// Base class for the options of <see cref="PipelineEventKinds"/>, using the CRTP pattern.
/// </summary>
public abstract class PipelineEventKindBase : TypeOptionBase<PipelineEventKindBase>, IPipelineEventKind
{
    /// <summary>Initializes a new instance of the <see cref="PipelineEventKindBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the kind reports.</param>
    protected PipelineEventKindBase(string name, string description)
        : base(name, name, description, "PipelineEventKinds")
    {
    }

    /// <inheritdoc />
    public abstract Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request);
}
