using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Operations.Endpoints;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// Regression tests for <see cref="GetDataSetLineageEndpointBase.BuildDownstreamConsumers"/>. The OLD code
/// filtered <c>pipe.Pipeline</c> directly on SourceDataSet/DestinationDataSet columns that do not exist
/// on that flat header table (SQL "Invalid column name", silently swallowed into an empty list by the
/// existing best-effort <c>QueryAll</c> pattern) — downstream consumers were ALWAYS empty. The fix loads
/// through the composing provider (<see cref="PipelineLineageLoader"/>) and filters in-memory.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public class GetDataSetLineageDownstreamConsumersTests
{
    private sealed class TestableEndpoint(
        DataSetConfigurationProvider dataSets, PipelineServiceConfigurationProvider provider,
        ICalculationConfigurationProvider calculations, ILogger<GetDataSetLineageEndpointBase> logger)
        : GetDataSetLineageEndpointBase(dataSets, provider, calculations, logger)
    {
        public Task<IReadOnlyList<LineageConsumerResponse>> InvokeBuildDownstreamConsumers(string dataSetName, CancellationToken ct) =>
            BuildDownstreamConsumers(dataSetName, ct);
    }

    private static GraphPipelineConfiguration ComposedPipeline(string name, string? reads, string? writes)
    {
        List<PipelineComponentConfiguration> components = [];
        if (reads is not null)
            components.Add(new PipelineComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = reads });

        if (writes is not null)
            components.Add(new PipelineComponentConfiguration { Name = "out", ComponentType = "DataSetDestination", DataSetName = writes });

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Domain = "Pipeline",
            Implementation = "Graph",
            IsEnabled = true,
            Components = components,
        };
    }

    // The providers are sealed and their reads are not virtual, so nothing can stand in for one. The
    // pipeline provider here is the real one: it reads the domain rows a faked store holds and dispatches
    // each to the implementation provider registered for its Implementation, the path production takes.
    // BuildDownstreamConsumers reads only through it; the DataSet provider is a constructor dependency.
    private static TestableEndpoint CreateEndpoint(params GraphPipelineConfiguration[] pipelines)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DomainConfiguration>>.Success(pipelines
                .Select(p => new DomainConfiguration { Id = p.Id, Name = p.Name, Domain = "Pipeline", Implementation = "Graph" })
                .ToList()));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(g => g.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        var implementations = new Mock<IImplementationConfigurationProvider<IPipelineImplementationConfiguration>>();
        foreach (var pipeline in pipelines)
        {
            implementations
                .Setup(i => i.Get(pipeline.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(pipeline));

            // A list read composes each member through the level-limited read (FDW-951).
            implementations
                .Setup(i => i.Get(pipeline.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(pipeline));
        }

        var provider = new PipelineServiceConfigurationProvider(
            NullLogger<PipelineServiceConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "pipe", "Pipeline");
        provider.Register("Graph", implementations.Object);

        var dataSets = new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "data", "DataSet");

        // Calculation consumers are read through the composed calculations; this store holds none.
        var calculations = new Mock<ICalculationConfigurationProvider>();
        calculations
            .Setup(c => c.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ICalculationEntityImplementationConfiguration>>.Success([]));

        return new TestableEndpoint(
            dataSets, provider, calculations.Object, NullLogger<GetDataSetLineageEndpointBase>.Instance);
    }

    [Fact]
    public async Task DownstreamConsumersPopulatedWhenPipelineConsumesDataSet()
    {
        var pipeline = ComposedPipeline("Consumer1", "UsgsDailySink", null);

        var consumers = await CreateEndpoint(pipeline)
            .InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);

        consumers.ShouldContain(c => c.Name == "Consumer1" && c.ConsumerType == "Pipeline (Graph)");
    }

    [Fact]
    public async Task DownstreamConsumersPopulatedWhenPipelineProducesDataSet()
    {
        var pipeline = ComposedPipeline("Producer1", null, "UsgsDailySink");

        var consumers = await CreateEndpoint(pipeline)
            .InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);

        consumers.ShouldContain(c => c.Name == "Producer1" && c.ConsumerType == "Pipeline Producer (Graph)");
    }

    [Fact]
    public async Task DownstreamConsumersEmptyWhenNoPipelineReferencesDataSet()
    {
        var pipeline = ComposedPipeline("Unrelated", "SomeOtherDataSet", "AnotherDataSet");

        var consumers = await CreateEndpoint(pipeline)
            .InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);

        consumers.ShouldBeEmpty();
    }

    [Fact]
    public async Task AGraphThatReadsSeveralDataSetsIsAConsumerOfEachOfThem()
    {
        var pipeline = ComposedPipeline("Joiner", "UsgsDailySink", "Summary");
        pipeline.Components.Add(
            new PipelineComponentConfiguration { Name = "second", ComponentType = "DataSetSource", DataSetName = "Teams" });

        var endpoint = CreateEndpoint(pipeline);

        var first = await endpoint.InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);
        var second = await endpoint.InvokeBuildDownstreamConsumers("Teams", TestContext.Current.CancellationToken);

        first.ShouldContain(c => c.Name == "Joiner" && c.ConsumerType == "Pipeline (Graph)");
        second.ShouldContain(c => c.Name == "Joiner" && c.ConsumerType == "Pipeline (Graph)");
    }

    [Fact]
    public async Task AGraphThatReadsAndWritesTheSameDataSetIsListedOnceAsAConsumer()
    {
        var pipeline = ComposedPipeline("Rewriter", "UsgsDailySink", "UsgsDailySink");

        var consumers = await CreateEndpoint(pipeline)
            .InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);

        consumers.Count(c => c.Name == "Rewriter").ShouldBe(1);
        consumers.ShouldContain(c => c.ConsumerType == "Pipeline (Graph)");
    }
}
