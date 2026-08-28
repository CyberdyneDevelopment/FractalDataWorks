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

    /// <inheritdoc />
    protected override IDataField BuildField(DataContainerFieldConfiguration fieldCfg)
        => new DataField(fieldCfg.Name, fieldCfg.Description, explicitType: null, fieldCfg.Ordinal, fieldCfg.IsNullable);

    /// <inheritdoc />
    protected override IDataContainer BuildContainer(
        DataContainerConfiguration containerCfg,
        IDataNodePath parent,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys)
    {
        var containerType = string.IsNullOrEmpty(containerCfg.TypeId)
            ? ContainerTypes.ByName("Endpoint")
            : ContainerTypes.ByName(containerCfg.TypeId);

        DataStoreLoaderLog.ContainerSubtypeChosen(Logger, containerCfg.Name, containerType.Name);
        DataStoreLoaderLog.ContainerFieldsBuilt(Logger, containerCfg.Name, fields.Count);

        return new DataContainer(
            containerCfg.Name,
            containerCfg.Description,
            parent,
            fields,
            keys,
            referencingKeys,
            containerType,
            ContainerComposition.ResolveFormat(containerCfg, _defaultResponseFormat),
            new GenericContainerPath(parent.Name),
            ["Query"],
            ContainerComposition.BuildMetadata(containerCfg),
            Logger);
    }
}
