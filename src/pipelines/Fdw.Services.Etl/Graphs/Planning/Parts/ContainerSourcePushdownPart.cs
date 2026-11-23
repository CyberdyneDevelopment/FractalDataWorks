using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// A container source pushed into its store (FDW-6969): the container read with no projection, its fields as the in-memory read returns them, in the
/// order of its primary key. A store that does not join, a container named otherwise than it is stored, or one with no primary key is read in memory.
/// </summary>
[TypeOption(typeof(PushdownParts), "ContainerSource")]
public sealed class ContainerSourcePushdownPart : PushdownPartBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSourcePushdownPart"/> class.</summary>
    public ContainerSourcePushdownPart() : base("ContainerSource", false, FlowGatewaySource.OutputName, string.Empty)
    {
    }

    /// <inheritdoc />
    public override async Task<PushdownAttempt> Resolve(
        PushdownScope scope, PipelineComponentConfiguration component, bool top, CancellationToken cancellationToken = default)
    {
        var settings = PushdownChecks.Plain(scope, component) is { Length: > 0 } plain ? plain : PushdownChecks.Source(component);
        if (settings.Length > 0 || component.DataStoreName is not { } dataStore || component.ContainerName is not { } containerName)
            return PushdownAttempt.Stays(settings);

        var target = new DataStoreTarget(dataStore, component.PathName, containerName);
        var described = await scope.Describe(target, cancellationToken).ConfigureAwait(false);
        if (described.IsFailure)
            return PushdownAttempt.Stays($"its container is not described for pushing: {described.FailureReason().Message}");

        if (described.Value is not { } container)
            return PushdownAttempt.Stays("the gateway answered its container with no description");

        var refusal = Refusal(target, container);
        return refusal.Length > 0
            ? PushdownAttempt.Stays(refusal)
            : PushdownAttempt.Pushed(new PushdownSide(
                container.DataStoreId,
                target,
                container.PrimaryKey,
                [.. container.Returned.Select(field => new PushdownColumn(field.Name, target.Container, field.Name, field.ClrType))],
                [],
                [.. container.PrimaryKey.Select(key => $"{target.Container}.{key}")],
                [target.Container],
                [component.Name],
                []));
    }

    /// <inheritdoc />
    /// <remarks>The data store record its container is described by (FDW-6970); empty, with the reason in the trace, when it is not described.</remarks>
    public override async Task<string> ReadStore(PushdownScope scope, PipelineComponentConfiguration component, CancellationToken cancellationToken = default)
    {
        if (component.DataStoreName is not { } dataStore || component.ContainerName is not { } containerName)
            return string.Empty;

        return await PushdownStores.Key(scope, component.Name, new DataStoreTarget(dataStore, component.PathName, containerName), cancellationToken).ConfigureAwait(false);
    }

    private static string Refusal(DataStoreTarget target, PushdownContainer container)
    {
        if (!container.JoinsInStore)
            return $"the connection of data store '{target.DataStore}' does not run joins in the store";
        if (!container.NamedAsStored || !PlainIdentifiers.IsPlain(target.Container))
            return $"container '{target.Container}' is not stored under its own plain name, which a joined statement names it by";
        if (container.PrimaryKey.Count == 0)
            return $"container '{target.Container}' declares no primary key, so the order its rows come in cannot be stated";
        return PushdownChecks.FirstNotPlain(container.Returned.Select(field => field.Name)) is { } name
            ? $"field '{name}' of container '{target.Container}' is not a name a statement writes as it is"
            : string.Empty;
    }
}
