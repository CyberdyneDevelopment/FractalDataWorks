using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Endpoints;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// Unit tests for <see cref="GetLineageGraphEndpointBase.BuildGraphFromRecords"/> — the edge kinds
/// produced from a pipeline's linkage-bearing <see cref="PipelineLineageRecord"/> projection and from
/// <see cref="DataSetSourceRecord.SourceDataSetName"/> (DerivesFrom).
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public class PipelineLineageEdgeTests
{
    private readonly Mock<ILogger> _logger = new();

    private static IReadOnlyList<T> Empty<T>() => [];

    [Fact]
    public void ConsumesEdgeCreatedFromSourceDataSet()
    {
        var pipelines = new List<PipelineLineageRecord>
        {
            new() { Id = Guid.NewGuid(), Name = "P1", ServiceOptionType = "Etl", SourceDataSet = "DS1" }
        };
        var dataSets = new List<DataSetRecord> { new() { Id = Guid.NewGuid(), Name = "DS1" } };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            dataSets, pipelines, Empty<DataSetSourceRecord>(), Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Edges.ShouldContain(e =>
            e.SourceId == "DataSet_DS1" && e.TargetId == "Pipeline_P1" && e.Type.Name == "Consumes");
    }

    [Fact]
    public void ProducesDataSetEdgeCreatedFromDestinationDataSet()
    {
        var pipelines = new List<PipelineLineageRecord>
        {
            new() { Id = Guid.NewGuid(), Name = "P1", ServiceOptionType = "Etl", DestinationDataSet = "DS2" }
        };
        var dataSets = new List<DataSetRecord> { new() { Id = Guid.NewGuid(), Name = "DS2" } };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            dataSets, pipelines, Empty<DataSetSourceRecord>(), Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Edges.ShouldContain(e =>
            e.SourceId == "Pipeline_P1" && e.TargetId == "DataSet_DS2" && e.Type.Name == "ProducesDataSet");
    }

    [Fact]
    public void WritesToEdgeCreatedFromDestinationConnection()
    {
        var pipelines = new List<PipelineLineageRecord>
        {
            new() { Id = Guid.NewGuid(), Name = "P1", ServiceOptionType = "Etl", DestinationConnectionName = "Conn1" }
        };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            Empty<DataSetRecord>(), pipelines, Empty<DataSetSourceRecord>(), Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Edges.ShouldContain(e =>
            e.SourceId == "Pipeline_P1" && e.TargetId == "Connection_Conn1" && e.Type.Name == "WritesTo");
    }

    [Fact]
    public void ReadsFromEdgeCreatedFromSourceConnection()
    {
        // Why: kind-4 edge — the source connection was previously collected only as a node,
        // never given an edge. See GetLineageGraphEndpointBase.AddSourceAndPipelineEdges.
        var pipelines = new List<PipelineLineageRecord>
        {
            new() { Id = Guid.NewGuid(), Name = "P1", ServiceOptionType = "Etl", SourceConnectionName = "ConnA" }
        };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            Empty<DataSetRecord>(), pipelines, Empty<DataSetSourceRecord>(), Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Edges.ShouldContain(e =>
            e.SourceId == "Connection_ConnA" && e.TargetId == "Pipeline_P1" && e.Type.Name == "ReadsFrom");
    }

    [Fact]
    public void DerivesFromEdgeCreatedFromSourceDataSetName()
    {
        // Why: kind-5 edge — data.DataSetSource.SourceDataSetName expresses derived-DataSet lineage,
        // never read by any graph before this fix.
        var ownerId = Guid.NewGuid();
        var dataSets = new List<DataSetRecord> { new() { Id = ownerId, Name = "DS_Owner" } };
        var sources = new List<DataSetSourceRecord>
        {
            new() { Id = Guid.NewGuid(), DataSetId = ownerId, SourceDataSetName = "DS_Upstream" }
        };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            dataSets, Empty<PipelineLineageRecord>(), sources, Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Edges.ShouldContain(e =>
            e.SourceId == "DataSet_DS_Upstream" && e.TargetId == "DataSet_DS_Owner" && e.Type.Name == "DerivesFrom");
    }

    [Fact]
    public void NoEdgesWhenLinkageAbsent()
    {
        // Why: absence-tolerance — a node-only pipeline (no composed engine body) must render as a
        // graph NODE with zero fabricated edges. NO FALLBACKS: linkage is never invented.
        var pipelines = new List<PipelineLineageRecord>
        {
            new() { Id = Guid.NewGuid(), Name = "Orphan", ServiceOptionType = "Etl" }
        };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            Empty<DataSetRecord>(), pipelines, Empty<DataSetSourceRecord>(), Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Nodes.ShouldContain(n => n.Id == "Pipeline_Orphan");
        graph.Edges.ShouldBeEmpty();
    }

    [Fact]
    public void DuplicateConnectionNodesDeduped()
    {
        var ds1 = Guid.NewGuid();
        var ds2 = Guid.NewGuid();
        var dataSets = new List<DataSetRecord> { new() { Id = ds1, Name = "DS1" }, new() { Id = ds2, Name = "DS2" } };
        var sources = new List<DataSetSourceRecord>
        {
            new() { Id = Guid.NewGuid(), DataSetId = ds1, ConnectionName = "SharedConn" },
            new() { Id = Guid.NewGuid(), DataSetId = ds2, ConnectionName = "SharedConn" }
        };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            dataSets, Empty<PipelineLineageRecord>(), sources, Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Nodes.Count(n => n.Id == "Connection_SharedConn").ShouldBe(1);
    }

    [Fact]
    public void DuplicateDerivesFromEdgesNotEmitted()
    {
        // Why: a source DataSet can be reused by multiple sibling sources of the same owner DataSet
        // (e.g. re-mapped per field-group); dedup so DerivesFrom is emitted once per distinct pair.
        var ownerId = Guid.NewGuid();
        var dataSets = new List<DataSetRecord> { new() { Id = ownerId, Name = "Owner" } };
        var sources = new List<DataSetSourceRecord>
        {
            new() { Id = Guid.NewGuid(), DataSetId = ownerId, SourceDataSetName = "Upstream" },
            new() { Id = Guid.NewGuid(), DataSetId = ownerId, SourceDataSetName = "Upstream" }
        };

        var graph = GetLineageGraphEndpointBase.BuildGraphFromRecords(
            dataSets, Empty<PipelineLineageRecord>(), sources, Empty<ChainDefinitionLineageRecord>(),
            Empty<ChainStepLineageRecord>(), Empty<ChainStepSourceFieldRecord>(),
            Empty<DataSetFieldMappingRecord>(), _logger.Object);

        graph.Edges.Count(e => e.Type.Name == "DerivesFrom").ShouldBe(1);
    }
}
