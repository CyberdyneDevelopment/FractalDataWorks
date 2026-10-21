using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Builds the node of each kind the map draws.</summary>
/// <remarks>
/// A value the configuration does not hold is left out of a node's metadata, not reported as an empty string: an empty
/// description or path would read as a recorded fact.
/// </remarks>
internal static class MapNodes
{
    /// <summary>A data set node, carrying its variations as tokens.</summary>
    /// <param name="dataSet">The data set.</param>
    /// <param name="variations">Its variations counted by kind, or none.</param>
    /// <param name="categoryPath">The path of the category the data set sits under, from the DataSet category tree; null when it sits under none.</param>
    public static MapNodeDto DataSet(
        IDataSetImplementationConfiguration dataSet, IReadOnlyList<WorkspaceMapVariationCountDto> variations, string? categoryPath) => new()
    {
        Id = MapNodeIds.DataSet(dataSet.Id),
        Label = dataSet.Name,
        Kind = MapKinds.DataSet,
        Category = categoryPath,
        CanExplode = true,
        Variations = variations,
        Metadata = Metadata(
            ("description", dataSet.Description),
            ("version", dataSet.Version),
            ("recordType", dataSet.RecordTypeName)),
    };

    /// <summary>A data store node.</summary>
    /// <param name="dataStore">The data store.</param>
    public static MapNodeDto DataStore(IDataStoreImplementationConfiguration dataStore) => new()
    {
        Id = MapNodeIds.DataStore(dataStore.Id),
        Label = dataStore.Name,
        Kind = MapKinds.DataStore,
        Category = dataStore.Implementation,
        CanExplode = true,
        Metadata = Metadata(
            ("description", dataStore.Description),
            ("isActive", dataStore.IsActive)),
    };

    /// <summary>A pipeline node.</summary>
    /// <param name="pipeline">The pipeline.</param>
    public static MapNodeDto Pipeline(IPipelineImplementationConfiguration pipeline) => new()
    {
        Id = MapNodeIds.Pipeline(pipeline.Id),
        Label = pipeline.Name,
        Kind = MapKinds.Pipeline,
        Category = pipeline.Implementation,
        CanExplode = false,
        Metadata = Metadata(
            ("isEnabled", pipeline is IEnablablePipelineConfiguration enablable ? (object)enablable.IsEnabled : null),
            ("organizationId", pipeline.OrgId?.ToString("D", CultureInfo.InvariantCulture))),
    };

    /// <summary>A container node, named within its data store.</summary>
    /// <param name="container">The container.</param>
    /// <param name="dataStoreId">The logical id of the data store it is in.</param>
    /// <param name="path">The path of the data store it sits under.</param>
    public static MapNodeDto Container(DataContainerConfiguration container, Guid dataStoreId, DataPathConfiguration path) => new()
    {
        Id = MapNodeIds.Container(container.Id),
        Label = container.Name,
        Kind = MapKinds.Container,
        CanExplode = false,
        Metadata = Metadata(
            ("dataStoreId", dataStoreId.ToString("D", CultureInfo.InvariantCulture)),
            ("path", path.PathValue),
            ("format", container.Format),
            ("description", container.Description)),
    };

    /// <summary>A data set field node, named within its data set.</summary>
    /// <param name="field">The field.</param>
    /// <param name="dataSetId">The logical id of the data set it belongs to.</param>
    public static MapNodeDto DataSetField(DataSetFieldConfiguration field, Guid dataSetId) => new()
    {
        Id = MapNodeIds.DataSetField(field.Id),
        Label = field.Name,
        Kind = MapKinds.DataSetField,
        CanExplode = false,
        Metadata = Metadata(
            ("dataSetId", dataSetId.ToString("D", CultureInfo.InvariantCulture)),
            ("typeName", field.TypeName),
            ("description", field.Description)),
    };

    /// <summary>A node for a resource of a kind the map does not read, as the workspace that holds it names it.</summary>
    /// <param name="resource">The resource attachment.</param>
    public static MapNodeDto Attached(WorkspaceResourceConfiguration resource) => new()
    {
        Id = MapNodeIds.Other(resource.ResourceType, resource.ResourceId),
        Label = resource.Name,
        Kind = resource.ResourceType,
        CanExplode = false,
        X = resource.X,
        Y = resource.Y,
        Metadata = Metadata(("relationship", resource.Relationship)),
    };

    private static Dictionary<string, object> Metadata(params (string Key, object? Value)[] entries)
        => entries
            .Where(entry => entry.Value is not null and not string { Length: 0 })
            .ToDictionary(entry => entry.Key, entry => entry.Value!, StringComparer.Ordinal);
}
