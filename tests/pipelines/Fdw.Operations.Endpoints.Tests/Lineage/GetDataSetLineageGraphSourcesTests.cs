using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Endpoints;
using Fdw.Services.Calculations;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// FDW-5296: a Graph data set has no source list, so the upstream sources of its lineage come from what its components read and what it only intends to read, the
/// latter marked. Every other kind answers nothing from this and keeps the sources it always had.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public class GetDataSetLineageGraphSourcesTests
{
    private sealed class TestableEndpoint()
        : GetDataSetLineageEndpointBase(null!, new Mock<IPipelineConfigurationProvider>().Object, new Mock<ICalculationConfigurationProvider>().Object, NullLogger<GetDataSetLineageEndpointBase>.Instance)
    {
        public IReadOnlyList<LineageSourceResponse> GraphSources(IDataSetImplementationConfiguration dataSet) => BuildGraphSources(dataSet);
    }

    [Fact]
    public void AGraphsComponentsAreItsUpstreamSourcesInTheOrderItReadsThem()
    {
        var graph = new GraphDataSetConfiguration
        {
            Name = "Revenue",
            Components =
            [
                new GraphDataSetComponentConfiguration { Name = "orders", ComponentType = "DataSetSource", DataSetName = "Orders" },
                new GraphDataSetComponentConfiguration { Name = "file", ComponentType = "ContainerSource", DataStoreName = "Landing", PathName = "inbound", ContainerName = "regions.csv" },
                new GraphDataSetComponentConfiguration { Name = "sql", ComponentType = "SqlSource", DataStoreName = "Warehouse" },
            ],
        };

        var sources = new TestableEndpoint().GraphSources(graph);

        sources.Select(source => (source.Name, source.SourceType, source.DataStoreName, source.PhysicalLocation, source.Priority)).ShouldBe(
        [
            ("Orders", "DataSet", null, null, 0),
            ("regions.csv", "Database", "Landing", "inbound.regions.csv", 1),
            ("Warehouse", "Database", "Warehouse", null, 2),
        ]);
        sources.ShouldAllBe(source => source.IsIntended == null);
    }

    [Fact]
    public void WhatAGraphOnlyIntendsToReadIsAnUpstreamSourceMarkedIntended()
    {
        var graph = new GraphDataSetConfiguration
        {
            Name = "Abstract",
            Intents =
            [
                new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "ExternalCrm", Ordinal = 0 },
                new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetId = Guid.NewGuid(), SourceDataSetName = "Prices", Ordinal = 1 },
            ],
        };

        var sources = new TestableEndpoint().GraphSources(graph);

        sources.Select(source => (source.Name, source.SourceType, source.IsIntended)).ShouldBe([("ExternalCrm", "Database", (bool?)true), ("Prices", "DataSet", (bool?)true)]);
    }

    [Fact]
    public void EveryOtherKindAnswersNoGraphSources()
    {
        var endpoint = new TestableEndpoint();

        endpoint.GraphSources(new SimpleDataSetConfiguration { Name = "Orders" }).ShouldBeEmpty();
        endpoint.GraphSources(new CompoundDataSetConfiguration { Name = "Orphan" }).ShouldBeEmpty();
        endpoint.GraphSources(new VariationDataSetConfiguration { Name = "ByDivision" }).ShouldBeEmpty();
    }
}
