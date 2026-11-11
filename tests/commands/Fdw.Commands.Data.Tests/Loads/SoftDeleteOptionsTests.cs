using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.SoftDeletes;

namespace Fdw.Commands.Data.Tests.Loads;

public sealed class SoftDeleteOptionsTests
{
    private static AbsentKeySet Keys(params string[] columns) =>
        new(columns, new List<IReadOnlyDictionary<string, object?>>());

    [Fact]
    public void MarkerMatchesNeedsAMarker() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { FlagValue = true }.FirstProblem()!.Message.ShouldContain("marker");

    [Fact]
    public void MarkerMatchesWithAMarkerIsConsistent() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { Marker = new SoftDeleteMarker("Status", "gone"), FlagValue = true }
            .FirstProblem().ShouldBeNull();

    [Fact]
    public void MarkerMatchesTakesNoSourceSet() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches)
        {
            Marker = new SoftDeleteMarker("Status", "gone"),
            AbsentFromSource = Keys("Code"),
            FlagValue = true,
        }.FirstProblem().ShouldNotBeNull();

    [Fact]
    public void AbsentFromSourceNeedsTheSourceSet() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource) { FlagValue = true }.FirstProblem().ShouldNotBeNull();

    [Fact]
    public void AbsentFromSourceTakesNoMarker() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource)
        {
            Marker = new SoftDeleteMarker("Status", "gone"),
            AbsentFromSource = Keys("Code"),
            FlagValue = true,
        }.FirstProblem().ShouldNotBeNull();

    [Fact]
    public void AbsentFromSourceNeedsAtLeastOneKeyColumn() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys(), FlagValue = true }
            .FirstProblem().ShouldNotBeNull();

    [Fact]
    public void FlagColumnIsRequired() =>
        new SoftDeleteOptions("", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys("Code"), FlagValue = true }
            .FirstProblem().ShouldNotBeNull();

    private static StagedKeySet Staged(long expected = 3, string stageKeyColumn = "StageKey", string stageKey = "run/dst", params string[] keys) =>
        new(Mock.Of<Fdw.Data.Abstractions.IStorageContainer>(), stageKeyColumn, stageKey, keys.Length == 0 ? ["Code"] : keys, expected);

    [Fact]
    public void AbsentFromStageWithItsStageIsConsistent() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromStage) { AbsentFromStage = Staged(), FlagValue = true }.FirstProblem().ShouldBeNull();

    [Fact]
    public void AbsentFromStageNeedsItsStage() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromStage) { FlagValue = true }.FirstProblem()!.Message.ShouldContain("names its stage");

    [Theory]
    [InlineData(0L, "StageKey", "run/dst", "at least one")]
    [InlineData(-1L, "StageKey", "run/dst", "at least one")]
    [InlineData(3L, " ", "run/dst", "stage key column")]
    [InlineData(3L, "StageKey", "", "states its stage key")]
    public void AbsentFromStageRefusesAStageItCannotTrust(long expected, string stageKeyColumn, string stageKey, string reason) =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromStage) { AbsentFromStage = Staged(expected, stageKeyColumn, stageKey), FlagValue = true }
            .FirstProblem()!.Message.ShouldContain(reason);

    [Fact]
    public void AbsentFromStageTakesNoSourceSet() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromStage) { AbsentFromStage = Staged(), AbsentFromSource = Keys("Code"), FlagValue = true }
            .FirstProblem().ShouldNotBeNull();

    [Fact]
    public void AbsentFromStageIsASourceSetModeAKindThatCannotStageRefuses() =>
        SoftDeleteModes.AbsentFromStage.NeedsSourceSet.ShouldBeTrue();

    [Fact]
    public void TheOtherModesTakeNoStage()
    {
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys("Code"), AbsentFromStage = Staged(), FlagValue = true }
            .FirstProblem().ShouldNotBeNull();
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { AbsentFromStage = Staged(), FlagValue = true }.FirstProblem().ShouldNotBeNull();
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { Marker = new SoftDeleteMarker("Status", "gone"), AbsentFromStage = Staged(), FlagValue = true }
            .FirstProblem().ShouldNotBeNull();
    }

    [Fact]
    public void FilterMatchesNeedsNothingMore() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { FlagValue = true }.FirstProblem().ShouldBeNull();

    [Fact]
    public void FilterMatchesTakesNoMarker() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { Marker = new SoftDeleteMarker("Status", "gone"), FlagValue = true }
            .FirstProblem()!.Message.ShouldContain("marker");

    [Fact]
    public void FilterMatchesTakesNoSourceSet() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { AbsentFromSource = Keys("Code"), FlagValue = true }
            .FirstProblem()!.Message.ShouldContain("source set");

    [Fact]
    public void TheFlagIsSetToTrueUnlessStated() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { FlagValue = true }.FlagValue.ShouldBeTrue();

    [Fact]
    public void FilterMatchesIsNotAnAbsentSet() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { FlagValue = true }.HasAbsentSet.ShouldBeFalse();

    [Fact]
    public void AMarkerModeAddsItsMarkerAsTheOnlyCondition()
    {
        var marker = new SoftDeleteMarker("Status", "gone");

        SoftDeleteModes.MarkerMatches.Conditions(new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { Marker = marker, FlagValue = true })
            .ShouldBe([marker]);
    }

    [Fact]
    public void TheFilterModeAndTheSourceSetModeAddNoCondition()
    {
        SoftDeleteModes.FilterMatches.Conditions(new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { FlagValue = true }).ShouldBeEmpty();
        SoftDeleteModes.AbsentFromSource.Conditions(new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys("Code"), FlagValue = true }).ShouldBeEmpty();
    }

    [Fact]
    public void OnlyTheSourceSetModeNeedsASourceSet()
    {
        SoftDeleteModes.AbsentFromSource.NeedsSourceSet.ShouldBeTrue();
        SoftDeleteModes.MarkerMatches.NeedsSourceSet.ShouldBeFalse();
        SoftDeleteModes.FilterMatches.NeedsSourceSet.ShouldBeFalse();
    }

    [Fact]
    public void ADeleteAndAnUpdateCarrySoftDeleteOptions()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys("Code"), FlagValue = true };

        new DeleteCommand { SoftDelete = options }.SoftDelete.ShouldBeSameAs(options);
        new UpdateCommand<object>(new object()) { SoftDelete = options }.SoftDelete.ShouldBeSameAs(options);
    }
}
