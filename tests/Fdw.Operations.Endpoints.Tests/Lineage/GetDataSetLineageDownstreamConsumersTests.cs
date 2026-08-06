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

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// Regression tests for <see cref="GetDataSetLineageEndpoint.BuildDownstreamConsumers"/>. The OLD code
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
        IConfigurationGateway gateway, PipelineServiceConfigurationProvider provider, ILogger<GetDataSetLineageEndpoint> logger)
        : GetDataSetLineageEndpoint(gateway, provider, logger)
    {
        public Task<IReadOnlyList<LineageConsumerResponse>> InvokeBuildDownstreamConsumers(string dataSetName, CancellationToken ct) =>
            BuildDownstreamConsumers(dataSetName, ct);
    }

    // Why: PipelineServiceConfigurationProvider is a concrete class (not an interface); Moq mocks it
    // via its public virtual Get(ct)/Get(id, ct) overloads. The Lazy<IConfigurationGateway> is never
    // dereferenced because both overloads are fully replaced by the mock setups below.
    private static Mock<PipelineServiceConfigurationProvider> CreateProviderMock()
    {
        return new Mock<PipelineServiceConfigurationProvider>(
            (ILogger<PipelineServiceConfigurationProvider>?)null!,
            new Lazy<IConfigurationGateway>(() => throw new InvalidOperationException(
                "BuildDownstreamConsumers must not touch the gateway directly - it only calls the provider.")),
            "ConfigurationDb",
            "pipe",
            (Lazy<ICacheInvalidator?>?)null!);
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

    private static TestableEndpoint CreateEndpoint(Mock<PipelineServiceConfigurationProvider> providerMock) =>
        new(Mock.Of<IConfigurationGateway>(), providerMock.Object, Mock.Of<ILogger<GetDataSetLineageEndpoint>>());

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
