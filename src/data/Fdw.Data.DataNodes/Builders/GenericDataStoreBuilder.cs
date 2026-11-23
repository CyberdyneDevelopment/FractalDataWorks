using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.DataNodes;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Builders;

/// <summary>
/// The generic per-transport <see cref="DataStoreBuilderBase"/> for non-SQL transports (HTTP, file).
/// Builds generic <see cref="DataContainer"/> nodes whose response <see cref="IStorageContainer.Format"/>
/// and row-shaping <see cref="IStorageContainer.Metadata"/> come from the container config's resolved
/// <c>FormatConfig</c> (via <see cref="ContainerComposition"/>), and whose physical address is a
/// <see cref="GenericContainerPath"/> carrying the owning path's request path.
/// </summary>
/// <remarks>
/// Why: replaces the generic branch of the deleted <c>DataStoreTreeBuilder</c>/<c>ConfigurationGateway</c>
/// builders and <c>DataStoreProvider.BuildCfgTierContainer</c>'s non-SQL branch. Used by any
/// <c>DataStoreType</c> whose transport is not a typed-field SQL backend (e.g. Http, FileSystem).
/// </remarks>
public sealed class GenericDataStoreBuilder : DataStoreBuilderBase
{
    private readonly IFormatType _defaultResponseFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericDataStoreBuilder"/> class.
    /// </summary>
    /// <param name="defaultResponseFormat">
    /// The transport's declared default response format (e.g. Http → Json), used to resolve a
    /// container's response format when its own configuration declares none. The caller obtains this
    /// from its own connection type option; pass <see cref="FormatTypes.NotFound"/> when the transport
    /// declares none — a missing default fails loud at read time rather than defaulting silently.
    /// </param>
    /// <param name="logger">Logger for build diagnostics.</param>
    public GenericDataStoreBuilder(IFormatType defaultResponseFormat, ILogger? logger = null)
        : base(logger)
    {
        _defaultResponseFormat = defaultResponseFormat;
    }

    /// <summary>
    /// Refuses a store whose configuration names a container type that ContainerTypes does not hold (FDW-6387): the lookup answers the
    /// NotFound placeholder then, and a container is never built with it as its type.
    /// </summary>
    /// <param name="config">The store configuration.</param>
    /// <returns>Success, or a failure naming the first container whose type is not registered.</returns>
    protected override IGenericResult ValidateConfiguration(IDataStoreImplementationConfiguration config)
    {
        foreach (var path in config.Paths)
        {
            foreach (var container in path.Containers)
            {
                // The type a container states is the rule's (FDW-6428, FDW-6387): stated and registered. This builder is transport-agnostic, so which types a store holds is
                // not asked here; the data store types' own builders and the store's save ask it with the store type.
                var stated = DataContainerTypeRule.Stated(container, Logger);
                if (stated.IsFailure)
                    return stated;
            }
        }

        return GenericResult.Success();
    }

    // ValidateConfiguration has refused a container with no TypeId, so the type is the one the configuration states. This builder is
    // transport-agnostic and serves every connection type's options, so it holds no own-option rule.
    private static IContainerType ContainerTypeOf(DataContainerConfiguration containerCfg)
        => ContainerTypes.ByName(containerCfg.TypeId!);

    /// <summary>Builds a generic <see cref="DataContainer"/> addressed by its owning path's request path.</summary>
    /// <param name="containerCfg">The container configuration row.</param>
    /// <param name="parent">The container's parent path node.</param>
    /// <param name="fields">This container's already-built fields.</param>
    /// <param name="keys">This container's already-resolved keys.</param>
    /// <param name="referencingKeys">Other containers' keys that reference this one, or a failure.</param>
    /// <returns>The built container.</returns>
    protected override IDataContainer BuildContainer(
        DataContainerConfiguration containerCfg,
        IDataNodePath parent,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys)
    {
        var containerType = ContainerTypeOf(containerCfg);

        DataStoreLoaderLog.ContainerSubtypeChosen(Logger, containerCfg.Name, containerType.Name);
        DataStoreLoaderLog.ContainerFieldsBuilt(Logger, containerCfg.Name, fields.Count);

        var format = ContainerComposition.ResolveFormat(containerCfg, _defaultResponseFormat);

        return new DataContainer(
            containerCfg.Name,
            containerCfg.Description,
            parent,
            fields,
            keys,
            referencingKeys,
            containerType,
            format,
            new GenericContainerPath(parent.Name),
            [.. containerType.SupportedOperations],
            ContainerComposition.BuildMetadata(containerCfg, format),
            Logger);
    }
}
