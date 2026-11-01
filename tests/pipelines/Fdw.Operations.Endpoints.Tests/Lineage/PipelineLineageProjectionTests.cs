using Fdw.Services.Etl.Graphs;
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

    private static PipelineComponentConfiguration Component(string name, string type, string? dataSet = null, string? store = null, string? container = null) => new()
    {
        Name = name, ComponentType = type, DataSetName = dataSet, DataStoreName = store, ContainerName = container,
    };

    private static GraphPipelineConfiguration Graph(params PipelineComponentConfiguration[] components) => new()
    {
        Id = System.Guid.NewGuid(),
        Name = "Load",
        Implementation = "Graph",
        IsEnabled = true,
        Components = [.. components],
    };

    [Fact]
    public void AGraphPipelineProjectsEveryDataSetAndContainerItReadsAndWrites()
    {
        var configuration = Graph(
            Component("a", "DataSetSource", dataSet: "Orders"),
            Component("b", "DataSetSource", dataSet: "Customers"),
            Component("c", "DataSetDestination", dataSet: "OrderSummary"),
            Component("d", "ContainerDestination", store: "Warehouse", container: "orders_out"));

        var record = PipelineLineageProjection.From(configuration, _logger.Object);

        record.Id.ShouldBe(configuration.Id);
        record.Implementation.ShouldBe("Graph");
        record.IsEnabled.ShouldBeTrue();
        record.DataSetsRead.ShouldBe(["Orders", "Customers"]);
        record.DataSetsWritten.ShouldBe(["OrderSummary"]);
        record.ContainersWritten.ShouldBe([new PipelineLineageContainer("Warehouse", "orders_out")]);
        record.ContainersRead.ShouldBeEmpty();
        VerifyLogged(LogLevel.Debug, 11017, Times.Once());
    }

    [Fact]
    public void AGraphPipelineThatWasNotReadWholeHasNoUsesAndIsNotInventedAny()
    {
        var configuration = new GraphPipelineConfiguration { Id = System.Guid.NewGuid(), Name = "Empty", Implementation = "Graph" };

        var record = PipelineLineageProjection.From(configuration, _logger.Object);

        record.Uses.ShouldBeEmpty();
        record.DataSetsRead.ShouldBeEmpty();
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
        record.Uses.ShouldBeEmpty();
        record.IsEnabled.ShouldBeFalse();
        VerifyLogged(LogLevel.Debug, 31002, Times.Once());
    }
}
