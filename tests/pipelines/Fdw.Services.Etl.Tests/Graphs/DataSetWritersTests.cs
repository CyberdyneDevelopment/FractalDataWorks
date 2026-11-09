using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-4488: which pipelines write each data set, worked out from the graphs alone: a component of a type with no outputs that names a data set writes it.
/// </summary>
public sealed class DataSetWritersTests
{
    private static GraphPipelineConfiguration Graph(string name, bool enabled, params (string Type, string? DataSet)[] components) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        IsEnabled = enabled,
        Components = [.. components.Select((c, i) => new PipelineComponentConfiguration { Name = "c" + i, ComponentType = c.Type, DataSetName = c.DataSet })]
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AnEnabledDestinationWritesItsDataSetAndASourceDoesNot()
    {
        var load = Graph("Load", true, ("DataSetSource", "Raw"), ("DataSetDestination", "Orders"));

        var writers = DataSetWriters.Of([load], NullLogger.Instance);

        writers["Orders"].ShouldBe([new DataSetWriter(load.Id, "Load", true)]);
        writers.ContainsKey("Raw").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SeveralPipelinesWritingOneDataSetAreAllListedAndTheNameIgnoresCase()
    {
        var first = Graph("First", true, ("DataSetDestination", "Orders"));
        var second = Graph("Second", false, ("DataSetDestination", "ORDERS"));

        var writers = DataSetWriters.Of([first, second], NullLogger.Instance);

        writers["orders"].Select(w => w.PipelineName).ShouldBe(["First", "Second"]);
        writers["orders"].Single(w => w.PipelineName == "Second").IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AWriterThatNamesItsDataSetByAPlaceholderIsLeftOut()
    {
        var generic = Graph("Generic", true, ("DataSetDestination", "{{Target}}"));
        var plain = Graph("Plain", true, ("DataSetDestination", "Orders"));

        var writers = DataSetWriters.Of([generic, plain], NullLogger.Instance);

        writers.Keys.ShouldBe(["Orders"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void APipelineThatWritesNothingAndNoPipelinesGiveNoWriters()
    {
        DataSetWriters.Of([Graph("Reader", true, ("DataSetSource", "Raw"))], NullLogger.Instance).ShouldBeEmpty();
        DataSetWriters.Of([], NullLogger.Instance).ShouldBeEmpty();
    }
}
