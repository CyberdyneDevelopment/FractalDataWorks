using Fdw.Data.DataSets;
using Fdw.Services.Workspaces.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.Ownership;

/// <summary>
/// FDW-6967: a path and a container of a shared store can be owned and are given back when their owner is archived; the store itself cannot be
/// owned, and a data set stays with the workspace that owns it.
/// </summary>
public sealed class OwnedStoreItemKindsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    public void APathAndAContainerCanBeOwnedAndAreReleasedOnArchive()
    {
        new DataPathResourceKind().CanBeOwned.ShouldBeTrue();
        new DataPathResourceKind().ReleasedOnArchive.ShouldBeTrue();
        new DataContainerResourceKind().CanBeOwned.ShouldBeTrue();
        new DataContainerResourceKind().ReleasedOnArchive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void TheStoreStaysSharedAndADataSetStaysWithItsWorkspace()
    {
        new DataStoreResourceKind().CanBeOwned.ShouldBeFalse();
        new DataSetResourceKind().ReleasedOnArchive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void OnlyAnArchivedWorkspaceIsClosed()
    {
        new ArchivedWorkspaceStatusOption().IsClosed.ShouldBeTrue();
        new ActiveWorkspaceStatusOption().IsClosed.ShouldBeFalse();
        new PausedWorkspaceStatusOption().IsClosed.ShouldBeFalse();
        new DraftWorkspaceStatusOption().IsClosed.ShouldBeFalse();
    }
}
