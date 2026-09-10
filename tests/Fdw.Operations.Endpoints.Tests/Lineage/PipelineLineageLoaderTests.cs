using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Endpoints;
using Fdw.Results;
using Fdw.Services.Etl.Pipelines;
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

    private static BatchCopyPipelineConfiguration Configured(string name, string sourceDataSet, string destinationDataSet) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Domain = "Pipeline",
        Implementation = "BatchCopy",
        IsEnabled = true,
        SourceDataSet = sourceDataSet,
        DestinationDataSet = destinationDataSet
    };

    private static Mock<IPipelineConfigurationProvider> ProviderReturning(
        IGenericResult<IReadOnlyList<IPipelineImplementationConfiguration>> result)
    {
        var provider = new Mock<IPipelineConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return provider;
    }

    [Fact]
    public async Task LoadProjectsLinkageFromEveryConfiguredPipeline()
    {
        var provider = ProviderReturning(
            GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success(
                [Configured("P1", "DS1", "DS2"), Configured("P2", "DS3", "DS4")]));

        var records = await PipelineLineageLoader.Load(
            provider.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.Count.ShouldBe(2);
        records.ShouldContain(r => r.Name == "P1" && r.SourceDataSet == "DS1" && r.DestinationDataSet == "DS2");
        records.ShouldContain(r => r.Name == "P2" && r.SourceDataSet == "DS3" && r.DestinationDataSet == "DS4");
        VerifyLogged(LogLevel.Debug, 11014, Times.Once());
        VerifyLogged(LogLevel.Debug, 11019, Times.Once());
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
        records[0].SourceDataSet.ShouldBeNull();
        VerifyLogged(LogLevel.Debug, 31002, Times.Once());
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
