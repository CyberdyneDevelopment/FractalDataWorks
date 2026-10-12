using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Operations.Endpoints;

/// <summary>Builds the node of each kind the dataflow graph draws.</summary>
/// <remarks>
/// A value the configuration does not hold is left out of a node's metadata, not reported as an
/// empty string: an empty description or path would read as a recorded fact.
/// </remarks>
internal static class DataflowNodes
{
    /// <summary>A data set node.</summary>
    public static DataflowNodeDto DataSet(DataSetRecord dataSet) => new()
    {
        Id = DataflowNodeIds.DataSet(dataSet.Id),
        Label = dataSet.Name,
        NodeType = "dataset",
        Category = dataSet.Category,
        Metadata = Metadata(
            ("version", dataSet.Version),
            ("description", dataSet.Description),
            ("recordType", dataSet.RecordTypeName))
    };

    /// <summary>A data set variation node, its category the kind of variation it is.</summary>
    public static DataflowNodeDto Variation(DataSetVariationDomainConfiguration variation) => new()
    {
        Id = DataflowNodeIds.Variation(variation.Id),
        Label = variation.Name,
        NodeType = "variation",
        Category = variation.Implementation,
        Metadata = Metadata(
            ("variationName", variation.VariationName),
            ("displayName", variation.DisplayName),
            ("description", variation.Description),
            ("ownerWorkspaceId", variation.OwnerWorkspaceId),
            ("createdAt", variation.CreatedAt))
    };

    /// <summary>A pipeline node.</summary>
    public static DataflowNodeDto Pipeline(PipelineLineageRecord pipeline) => new()
    {
        Id = DataflowNodeIds.Pipeline(pipeline.Name),
        Label = pipeline.Name,
        NodeType = "pipeline",
        Category = pipeline.Implementation,
        Metadata = Metadata(("isEnabled", pipeline.IsEnabled))
    };

    /// <summary>A datastore node.</summary>
    public static DataflowNodeDto DataStore(DataStoreRecord store) => new()
    {
        Id = DataflowNodeIds.DataStore(store.Id),
        Label = store.Name,
        NodeType = "datastore",
        Category = store.Implementation,
        // location and translatorType are gone rather than defaulted: data.DataStore
        // carries neither, and where a store lives is the connection's business now.
        Metadata = Metadata(
            ("storeType", store.Implementation),
            ("description", store.Description))
    };

    /// <summary>A container node, named within its datastore.</summary>
    public static DataflowNodeDto Container(string dataStoreName, string containerName) => new()
    {
        Id = DataflowNodeIds.Container(dataStoreName, containerName),
        Label = containerName,
        NodeType = "container",
        Category = dataStoreName
    };

    /// <summary>A data set source node. Only the sources layer draws these.</summary>
    public static DataflowNodeDto Source(DataSetSourceConfiguration source) => new()
    {
        Id = DataflowNodeIds.Source(source.Id),
        Label = source.SourceName,
        NodeType = "source",
        Category = source.ConnectionType,
        Metadata = Metadata(
            ("connectionName", source.ConnectionName),
            ("dataStoreName", source.DataStoreName),
            ("priority", source.Priority),
            ("path", source.PathValue),
            ("containerName", source.ContainerName),
            ("httpEndpoint", source.HttpEndpoint),
            ("filePath", source.FilePath))
    };

    /// <summary>A connection node. Only the sources layer draws these.</summary>
    public static DataflowNodeDto Connection(string name) => new()
    {
        Id = DataflowNodeIds.Connection(name),
        Label = name,
        NodeType = "connection"
    };

    private static Dictionary<string, object> Metadata(params (string Key, object? Value)[] entries)
        => entries
            .Where(e => e.Value is not null)
            .ToDictionary(e => e.Key, e => e.Value!, StringComparer.Ordinal);
}
