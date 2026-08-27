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
/// Unit tests for <see cref="PipelineLineageLoader"/> — the list-headers-then-per-header-<c>Get(id)</c>-
/// compose (N+1) mechanism required because <see cref="PipelineServiceConfigurationProvider"/>'s list
/// overload returns headers only (does not call <c>ComposeTypedBody</c>/<c>ComposeChildren</c>).
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

    // Why: PipelineServiceConfigurationProvider is a concrete class (not an interface); Moq mocks it
    // via its public virtual Get(ct)/Get(id, ct) overloads. The gateway provider is never
    // dereferenced because both overloads are fully replaced by the mock setups below.
    private static Mock<PipelineServiceConfigurationProvider> CreateProviderMock()
    {
        return new Mock<PipelineServiceConfigurationProvider>(
            (ILogger<PipelineServiceConfigurationProvider>?)null!,
            new ConfigurationGatewayProvider(),
            "ConfigurationDb",
            "pipe");
    }

    private static PipelineConfiguration Header(string name) =>
        new() { Id = Guid.NewGuid(), Name = name, ServiceOptionType = "Etl" };

    private static PipelineConfiguration Composed(PipelineConfiguration header, string sourceDataSet, string destinationDataSet) => new()
    {
        Id = header.Id,
        Name = header.Name,
        ServiceOptionType = header.ServiceOptionType,
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

    [Fact]
    public async Task LoadComposesEachHeaderAndProjectsLinkage()
    {
        var h1 = Header("P1");
        var h2 = Header("P2");
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineConfiguration>>.Success([h1, h2]));
        providerMock.Setup(p => p.Get(h1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineConfiguration>.Success(Composed(h1, "DS1", "DS2")));
        providerMock.Setup(p => p.Get(h2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineConfiguration>.Success(Composed(h2, "DS3", "DS4")));

        var records = await PipelineLineageLoader.Load(
            providerMock.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.Count.ShouldBe(2);
        records.ShouldContain(r => r.Name == "P1" && r.SourceDataSet == "DS1" && r.DestinationDataSet == "DS2");
        records.ShouldContain(r => r.Name == "P2" && r.SourceDataSet == "DS3" && r.DestinationDataSet == "DS4");
        providerMock.Verify(p => p.Get(h1.Id, It.IsAny<CancellationToken>()), Times.Once);
        providerMock.Verify(p => p.Get(h2.Id, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged(LogLevel.Debug, 11014, Times.Once());
        VerifyLogged(LogLevel.Trace, 11015, Times.Exactly(2));
        VerifyLogged(LogLevel.Debug, 11019, Times.Once());
    }

    [Fact]
    public async Task ComposeFailureRendersNodeOnlyAndDoesNotThrow()
    {
        var header = Header("Broken");
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineConfiguration>>.Success([header]));
        providerMock.Setup(p => p.Get(header.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineConfiguration>.Failure(new GenericMessage("compose failed")));

        var records = await PipelineLineageLoader.Load(
            providerMock.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.Count.ShouldBe(1);
        records[0].Name.ShouldBe("Broken");
        records[0].SourceDataSet.ShouldBeNull();
        VerifyLogged(LogLevel.Error, 31003, Times.Once());
    }

    [Fact]
    public async Task NoHeadersReturnsEmptyListWithoutThrow()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineConfiguration>>.Success([]));

        var records = await PipelineLineageLoader.Load(
            providerMock.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.ShouldBeEmpty();
        providerMock.Verify(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailedHeaderListReturnsEmptyRecordsWithoutThrow()
    {
        // Why: NO FALLBACKS — a failed header list renders as an empty (honest) record set, never a
        // fabricated placeholder pipeline.
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineConfiguration>>.Failure(new GenericMessage("list failed")));

        var records = await PipelineLineageLoader.Load(
            providerMock.Object, _logger.Object, TestContext.Current.CancellationToken);

        records.ShouldBeEmpty();
    }
}
