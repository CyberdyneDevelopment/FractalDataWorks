using System.Collections.Generic;
using Fdw.Services.Connections;
using Fdw.Services.Workspaces.Endpoints;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>
/// FDW-6391: a data store names its working areas, and a path or schema name is in one when it equals a name the store states, by name alone: nothing is looked up
/// in the store's catalog, so a table that was dropped or never discovered is judged the same. A store that states none has none.
/// </summary>
public class WorkingAreasTests
{
    private static IDataStoreImplementationConfiguration Store(params string[] workingPaths)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>(MockBehavior.Strict);
        store.SetupGet(s => s.WorkingPaths).Returns([.. workingPaths]);
        return store.Object;
    }

    [Theory]
    [InlineData("tmp")]
    [InlineData("TMP")]
    [InlineData("Tmp")]
    public void ANameTheStoreStatesIsInAWorkingAreaWhateverItsCase(string name)
    {
        WorkingAreas.Contains(Store("tmp"), name).ShouldBeTrue();
    }

    [Fact]
    public void ANameTheStoreDoesNotStateIsNotInOne()
    {
        WorkingAreas.Contains(Store("tmp"), "dbo").ShouldBeFalse();
    }

    [Fact]
    public void AStoreThatStatesNoneHasNone()
    {
        WorkingAreas.Contains(Store(), "tmp").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ANameWithNoPathIsNeverInAWorkingArea(string? name)
    {
        WorkingAreas.Contains(Store("tmp"), name).ShouldBeFalse();
    }

    [Fact]
    public void ANameIsMatchedWholeNotByItsStart()
    {
        WorkingAreas.Contains(Store("tmp"), "tmp_archive").ShouldBeFalse();
    }
}
