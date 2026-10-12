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
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches).FirstProblem()!.ShouldContain("marker");

    [Fact]
    public void MarkerMatchesWithAMarkerIsConsistent() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { Marker = new SoftDeleteMarker("Status", "gone") }
            .FirstProblem().ShouldBeNull();

    [Fact]
    public void MarkerMatchesTakesNoSourceSet() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches)
        {
            Marker = new SoftDeleteMarker("Status", "gone"),
            AbsentFromSource = Keys("Code"),
        }.FirstProblem().ShouldNotBeNull();

    [Fact]
    public void AbsentFromSourceNeedsTheSourceSet() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource).FirstProblem().ShouldNotBeNull();

    [Fact]
    public void AbsentFromSourceTakesNoMarker() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource)
        {
            Marker = new SoftDeleteMarker("Status", "gone"),
            AbsentFromSource = Keys("Code"),
        }.FirstProblem().ShouldNotBeNull();

    [Fact]
    public void AbsentFromSourceNeedsAtLeastOneKeyColumn() =>
        new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys() }
            .FirstProblem().ShouldNotBeNull();

    [Fact]
    public void FlagColumnIsRequired() =>
        new SoftDeleteOptions("", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys("Code") }
            .FirstProblem().ShouldNotBeNull();

    [Fact]
    public void ADeleteAndAnUpdateCarrySoftDeleteOptions()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource) { AbsentFromSource = Keys("Code") };

        new DeleteCommand { SoftDelete = options }.SoftDelete.ShouldBeSameAs(options);
        new UpdateCommand<object>(new object()) { SoftDelete = options }.SoftDelete.ShouldBeSameAs(options);
    }
}
