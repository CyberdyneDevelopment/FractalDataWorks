using Fdw.Services.Etl;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// Unit tests for <see cref="PipelineLineageProjection"/> — the dot-walk from a composed
/// <see cref="PipelineConfiguration"/> aggregate (header → EtlPipeline kind body → engine typed body)
/// to the flat <see cref="PipelineLineageRecord"/> the lineage graph builder consumes.
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
    public void FromComposedBatchCopyAggregateExtractsLinkage()
    {
        var aggregate = new PipelineConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "UsgsDailyLoad",
            ServiceOptionType = "Etl",
            Configuration = new EtlPipelineConfiguration
            {
                ServiceOptionType = "BatchCopy",
                Configuration = new BatchCopyPipelineConfiguration
                {
                    IsEnabled = true,
                    SourceDataSet = "UsgsDailyRaw",
                    DestinationDataSet = "UsgsDailySink",
                    SourceConnectionName = "UsgsHttp",
                    DestinationConnectionName = "NflDb"
                }
            }
        };

        var record = PipelineLineageProjection.From(aggregate, _logger.Object);

        record.Id.ShouldBe(aggregate.Id);
        record.Name.ShouldBe("UsgsDailyLoad");
        record.ServiceOptionType.ShouldBe("Etl");
        record.SourceDataSet.ShouldBe("UsgsDailyRaw");
        record.DestinationDataSet.ShouldBe("UsgsDailySink");
        record.SourceConnectionName.ShouldBe("UsgsHttp");
        record.DestinationConnectionName.ShouldBe("NflDb");
        record.IsEnabled.ShouldBeTrue();
        VerifyLogged(LogLevel.Debug, 11016, Times.Once());
        VerifyLogged(LogLevel.Debug, 11017, Times.Once());
    }

    [Fact]
    public void FromComposedStreamingAggregateExtractsLinkage()
    {
        // Why: proves polymorphism — the projection reads linkage off IEtlPipelineTypedConfiguration,
        // never a `is BatchCopyPipelineConfiguration` branch.
        var aggregate = new PipelineConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "StreamingIngest",
            ServiceOptionType = "Etl",
            Configuration = new EtlPipelineConfiguration
            {
                ServiceOptionType = "Streaming",
                Configuration = new StreamingPipelineConfiguration
                {
                    IsEnabled = true,
                    SourceDataSet = "StreamSourceDs",
                    DestinationDataSet = "StreamSinkDs",
                    SourceConnectionName = "StreamSourceConn",
                    DestinationConnectionName = "StreamSinkConn"
                }
            }
        };

        var record = PipelineLineageProjection.From(aggregate, _logger.Object);

        record.SourceDataSet.ShouldBe("StreamSourceDs");
        record.DestinationDataSet.ShouldBe("StreamSinkDs");
        record.SourceConnectionName.ShouldBe("StreamSourceConn");
        record.DestinationConnectionName.ShouldBe("StreamSinkConn");
        record.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void FromHeaderWithNoEngineBodyReturnsNodeOnlyRecordAndLogs()
    {
        var aggregate = new PipelineConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "OrphanPipeline",
            ServiceOptionType = "Etl",
            Configuration = new EtlPipelineConfiguration
            {
                ServiceOptionType = "BatchCopy",
                Configuration = null
            }
        };

        var record = PipelineLineageProjection.From(aggregate, _logger.Object);

        record.Name.ShouldBe("OrphanPipeline");
        record.Id.ShouldBe(aggregate.Id);
        record.SourceDataSet.ShouldBeNull();
        record.DestinationDataSet.ShouldBeNull();
        record.SourceConnectionName.ShouldBeNull();
        record.DestinationConnectionName.ShouldBeNull();
        record.IsEnabled.ShouldBeFalse();
        VerifyLogged(LogLevel.Debug, 31002, Times.Once());
    }

    [Fact]
    public void FromHeaderWithNonEtlKindReturnsNodeOnly()
    {
        var aggregate = new PipelineConfiguration
        {
            Id = System.Guid.NewGuid(),
            Name = "NonEtlPipeline",
            ServiceOptionType = "SomeOtherKind",
            Configuration = null
        };

        var record = PipelineLineageProjection.From(aggregate, _logger.Object);

        record.Name.ShouldBe("NonEtlPipeline");
        record.SourceDataSet.ShouldBeNull();
        record.DestinationDataSet.ShouldBeNull();
        VerifyLogged(LogLevel.Debug, 31002, Times.Once());
    }
}
