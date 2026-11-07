using System.Linq;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// FDW-4278: a PipelineRun threshold states which outcomes of a run it counts, as a required list parameter, so a threshold that does not state it is refused where
/// the parameters are read.
/// </summary>
public sealed class PipelineRunThresholdTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheTypeDeclaresCountedOutcomesAsARequiredListOfText()
    {
        var declared = new PipelineRunThresholdType().Parameters.ShouldHaveSingleItem();

        declared.Name.ShouldBe("CountedOutcomes");
        declared.IsRequired.ShouldBeTrue();
        declared.IsList.ShouldBeTrue();
        declared.ValueType.ShouldBe(typeof(string));
    }
}
