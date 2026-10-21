using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Endpoints;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// Unit tests for <see cref="PipelineLineageLoader"/> — one read through
/// <see cref="IPipelineConfigurationProvider"/>, whose list overload dispatches each row to the
/// implementation it names, projected onto the flat lineage records.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public class PipelineLineageLoaderTests
{
    private readonly Mock<ILogger> _logger = new();

    public PipelineLineageLoaderTests()
    {
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    private void VerifyLogged(LogLevel level, int eventId, Times times) =>
        _logger.Verify(
            l => l.Log(
                level,
                new EventId(eventId),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    private static Mock<IPipelineConfigurationProvider> ProviderReturning(
        IGenericResult<IReadOnlyList<IPipelineImplementationConfiguration>> result)
    {
        var provider = new Mock<IPipelineConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return provider;
    }

    [Fact]
    public async Task AnImplementationCarryingNoEtlLinkageRendersNodeOnly()
    {
        var provider = ProviderReturning(
            GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success(
                [new NonEtlPipelineConfiguration { Id = Guid.NewGuid(), Name = "Broken", Implementation = "SomeOtherKind" }]));

        var records = await PipelineLineageLoader.Load(
            provider.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.Count.ShouldBe(1);
        records[0].Name.ShouldBe("Broken");
        records[0].Uses.ShouldBeEmpty();
        VerifyLogged(LogLevel.Debug, 31002, Times.Once());
    }

    private static GraphPipelineConfiguration Whole(Guid id) => new()
    {
        Id = id,
        Name = "Load",
        Implementation = "Graph",
        Components =
        [
            new PipelineComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = "Orders" },
            new PipelineComponentConfiguration { Name = "out", ComponentType = "DataSetDestination", DataSetName = "OrderSummary" },
        ],
    };

    [Fact]
    public async Task AGraphPipelineListedAsARecordIsReadWholeSoItsUsesAreKnown()
    {
        var id = Guid.NewGuid();
        var listed = new GraphPipelineConfiguration { Id = id, Name = "Load", Implementation = "Graph" };
        var provider = ProviderReturning(
            GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([listed]));
        provider
            .Setup(p => p.Get(id, ConfigurationLevels.WholeAggregate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(Whole(id)));

        var records = await PipelineLineageLoader.Load(provider.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.Count.ShouldBe(1);
        records[0].DataSetsRead.ShouldBe(["Orders"]);
        records[0].DataSetsWritten.ShouldBe(["OrderSummary"]);
    }

    [Fact]
    public async Task AFailedWholeReadOfOnePipelineFailsTheReadInsteadOfDrawingItWithNoLines()
    {
        var id = Guid.NewGuid();
        var listed = new GraphPipelineConfiguration { Id = id, Name = "Load", Implementation = "Graph" };
        var provider = ProviderReturning(
            GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([listed]));
        provider
            .Setup(p => p.Get(id, ConfigurationLevels.WholeAggregate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Failure(new GenericMessage("steps failed")));

        var read = await PipelineLineageLoader.Read(provider.Object, _logger.Object, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task NoConfiguredPipelinesReturnsEmptyListWithoutThrow()
    {
        var provider = ProviderReturning(
            GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([]));

        var records = await PipelineLineageLoader.Load(
            provider.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFailedReadReturnsEmptyRecordsAndLogsWithoutThrow()
    {
        var provider = ProviderReturning(
            GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Failure(new GenericMessage("list failed")));

        var records = await PipelineLineageLoader.Load(
            provider.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.ShouldBeEmpty();
        VerifyLogged(LogLevel.Error, 31003, Times.Once());
    }
}
