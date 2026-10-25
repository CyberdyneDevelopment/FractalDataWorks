using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for when a destination holds its batches back (FDW-1647): a soft delete flags the keys absent from the rows it is given, so
/// it must be given them all; any other load runs as each batch arrives.
/// </summary>
public sealed class FlowLoadDestinationTests
{
    private static LoadUnit Unit(string? softDeleteMode)
    {
        var kind = LoadStepKinds.ByName("Upsert");
        var row = new PipelineLoadStepConfiguration { Name = "load", Phase = "Load", Kind = kind.Name, IsEnabled = true, SoftDeleteMode = softDeleteMode };
        return LoadUnit.ForDataSet("graph destination 'load'", kind, row, "Orders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoadThatStatesASoftDeleteIsGivenEveryRowAtOnce()
    {
        FlowLoadDestination.NeedsWholeSet(Unit("AbsentFromSource")).ShouldBeTrue();
        FlowLoadDestination.NeedsWholeSet(Unit("MarkerMatches")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoadWithNoSoftDeleteRunsAsEachBatchArrives()
    {
        FlowLoadDestination.NeedsWholeSet(Unit(null)).ShouldBeFalse();
        FlowLoadDestination.NeedsWholeSet(Unit(" ")).ShouldBeFalse();
    }
}
