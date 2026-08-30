using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Endpoints;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

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
        DataSetConfigurationProvider dataSets, PipelineServiceConfigurationProvider provider, ILogger<GetDataSetLineageEndpointBase> logger)
        : GetDataSetLineageEndpointBase(dataSets, provider, logger)
    {
        public Task<IReadOnlyList<LineageConsumerResponse>> InvokeBuildDownstreamConsumers(string dataSetName, CancellationToken ct) =>
            BuildDownstreamConsumers(dataSetName, ct);
    }

    private static Mock<PipelineServiceConfigurationProvider> CreateProviderMock()
    {
        return new Mock<PipelineServiceConfigurationProvider>(
            (ILogger<PipelineServiceConfigurationProvider>?)null!,
            new ConfigurationGatewayProvider(),
            "PlatformConfiguration",
            "pipe");
    }

    private static PipelineConfiguration ComposedPipeline(string name, string sourceDataSet, string destinationDataSet) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        ServiceOptionType = "Etl",
        Configuration = new EtlPipelineConfiguration
        {
            ServiceOptionType = "BatchCopy",
            Configuration = new BatchCopyPipelineConfiguration
            {
                IsEnabled = true,
                SourceDataSet = sourceDataSet,
                DestinationDataSet = destinationDataSet
            }
        }
    };

    // These tests only exercise BuildDownstreamConsumers, which reads through the pipeline provider;
    // the DataSet provider is a constructor dependency it never touches here.
    private static TestableEndpoint CreateEndpoint(Mock<PipelineServiceConfigurationProvider> providerMock) =>
        new(new Mock<DataSetConfigurationProvider>(
                    (ILogger<DataSetConfigurationProvider>?)null!,
                    new ConfigurationGatewayProvider(),
                    "PlatformConfiguration",
                    "data").Object,
            providerMock.Object,
            Mock.Of<ILogger<GetDataSetLineageEndpointBase>>());

    [Fact]
    public async Task DownstreamConsumersPopulatedWhenPipelineConsumesDataSet()
    {
        var pipeline = ComposedPipeline("Consumer1", "UsgsDailySink", string.Empty);
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineConfiguration>>.Success([pipeline]));
        providerMock.Setup(p => p.Get(pipeline.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineConfiguration>.Success(pipeline));

        var consumers = await CreateEndpoint(providerMock)
            .InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);

        consumers.ShouldContain(c => c.Name == "Consumer1" && c.ConsumerType.Contains("Pipeline (", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DownstreamConsumersPopulatedWhenPipelineProducesDataSet()
    {
        var pipeline = ComposedPipeline("Producer1", string.Empty, "UsgsDailySink");
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineConfiguration>>.Success([pipeline]));
        providerMock.Setup(p => p.Get(pipeline.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineConfiguration>.Success(pipeline));

        var consumers = await CreateEndpoint(providerMock)
            .InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);

        consumers.ShouldContain(c => c.Name == "Producer1" && c.ConsumerType.Contains("Producer", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DownstreamConsumersEmptyWhenNoPipelineReferencesDataSet()
    {
        var pipeline = ComposedPipeline("Unrelated", "SomeOtherDataSet", "AnotherDataSet");
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineConfiguration>>.Success([pipeline]));
        providerMock.Setup(p => p.Get(pipeline.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineConfiguration>.Success(pipeline));

        var consumers = await CreateEndpoint(providerMock)
            .InvokeBuildDownstreamConsumers("UsgsDailySink", TestContext.Current.CancellationToken);

        consumers.ShouldBeEmpty();
    }
}
