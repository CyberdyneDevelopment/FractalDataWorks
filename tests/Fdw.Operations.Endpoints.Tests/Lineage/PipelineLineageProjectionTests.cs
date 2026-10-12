using Fdw.Services.Etl.Pipelines;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// Unit tests for <see cref="PipelineLineageProjection"/> — the read from a configured pipeline, as
/// the Pipeline domain hands it back, to the flat <see cref="PipelineLineageRecord"/> the lineage
/// graph builder consumes.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public class PipelineLineageProjectionTests
{
    private readonly Mock<ILogger> _logger = new();

    public PipelineLineageProjectionTests()
    {
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    private void VerifyLogged(LogLevel level, int eventId, Times times) =>
        _logger.Verify(
            l => l.Log(
                level,
                new EventId(eventId),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<System.Exception?>(),
                It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            times);

    [Fact]
    public void FromBatchCopyImplementationExtractsLinkage()
    {
        var configuration = new BatchCopyPipelineConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "UsgsDailyLoad",
            Domain = "Pipeline",
            Implementation = "BatchCopy",
            IsEnabled = true,
            SourceDataSet = "UsgsDailyRaw",
            DestinationDataSet = "UsgsDailySink",
            SourceConnectionName = "UsgsHttp",
            DestinationConnectionName = "NflDb"
        };

        var record = PipelineLineageProjection.From(configuration, _logger.Object);

        record.Id.ShouldBe(configuration.Id);
        record.Name.ShouldBe("UsgsDailyLoad");
        record.Implementation.ShouldBe("BatchCopy");
        record.SourceDataSet.ShouldBe("UsgsDailyRaw");
        record.DestinationDataSet.ShouldBe("UsgsDailySink");
        record.SourceConnectionName.ShouldBe("UsgsHttp");
        record.DestinationConnectionName.ShouldBe("NflDb");
        record.IsEnabled.ShouldBeTrue();
        VerifyLogged(LogLevel.Debug, 11016, Times.Once());
        VerifyLogged(LogLevel.Debug, 11017, Times.Once());
    }

    [Fact]
    public void FromStreamingImplementationExtractsLinkage()
    {
        var configuration = new StreamingPipelineConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "StreamingIngest",
            Domain = "Pipeline",
            Implementation = "Streaming",
            IsEnabled = true,
            SourceDataSet = "StreamSourceDs",
            DestinationDataSet = "StreamSinkDs",
            SourceConnectionName = "StreamSourceConn",
            DestinationConnectionName = "StreamSinkConn"
        };

        var record = PipelineLineageProjection.From(configuration, _logger.Object);

        record.SourceDataSet.ShouldBe("StreamSourceDs");
        record.DestinationDataSet.ShouldBe("StreamSinkDs");
        record.SourceConnectionName.ShouldBe("StreamSourceConn");
        record.DestinationConnectionName.ShouldBe("StreamSinkConn");
        record.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void FromAnImplementationCarryingNoEtlLinkageReturnsNodeOnlyRecordAndLogs()
    {
        var configuration = new NonEtlPipelineConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "NonEtlPipeline",
            Domain = "Pipeline",
            Implementation = "SomeOtherKind"
        };

        var record = PipelineLineageProjection.From(configuration, _logger.Object);

        record.Name.ShouldBe("NonEtlPipeline");
        record.Id.ShouldBe(configuration.Id);
        record.Implementation.ShouldBe("SomeOtherKind");
        record.SourceDataSet.ShouldBeNull();
        record.DestinationDataSet.ShouldBeNull();
        record.SourceConnectionName.ShouldBeNull();
        record.DestinationConnectionName.ShouldBeNull();
        record.IsEnabled.ShouldBeFalse();
        VerifyLogged(LogLevel.Debug, 31002, Times.Once());
    }
}
