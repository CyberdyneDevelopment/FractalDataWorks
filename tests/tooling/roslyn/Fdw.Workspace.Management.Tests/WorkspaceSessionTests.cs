using Fdw.Workspace.Management;

namespace Fdw.Workspace.Management.Tests;

[Collection(nameof(WorkspaceTestCollection))]
public class WorkspaceSessionTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WorkspaceSession_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var session = new WorkspaceSession();

        // Assert
        session.Id.ShouldBe(Guid.Empty);
        session.WorkspaceId.ShouldBe(Guid.Empty);
        session.SolutionPath.ShouldBe(string.Empty);
        session.Name.ShouldBe(string.Empty);
        session.Snapshots.ShouldNotBeNull();
        session.Snapshots.ShouldBeEmpty();
        session.Metadata.ShouldNotBeNull();
        session.Metadata.ShouldBeEmpty();
        session.Version.ShouldBe(1);
        session.BaselineSnapshot.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void SnapshotRecord_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var record = new SnapshotRecord();

        // Assert
        record.Id.ShouldBe(string.Empty);
        record.Name.ShouldBe(string.Empty);
        record.Description.ShouldBeNull();
        record.DocumentChanges.ShouldNotBeNull();
        record.DocumentChanges.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void SnapshotRecord_CanStoreDocumentChanges()
    {
        // Arrange
        var record = new SnapshotRecord
        {
            Id = "test-snapshot",
            Name = "Test Snapshot",
            Description = "A test snapshot",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        record.DocumentChanges["file1.cs"] = "// content 1";
        record.DocumentChanges["file2.cs"] = "// content 2";

        // Assert
        record.DocumentChanges.Count.ShouldBe(2);
        record.DocumentChanges["file1.cs"].ShouldBe("// content 1");
        record.DocumentChanges["file2.cs"].ShouldBe("// content 2");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WorkspaceSession_CanAddSnapshots()
    {
        // Arrange
        var session = new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = @"C:\Test\Solution.sln",
            Name = "TestSolution"
        };

        var snapshot = new SnapshotRecord
        {
            Id = "snapshot-1",
            Name = "First Snapshot",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        session.Snapshots.Add(snapshot);

        // Assert
        session.Snapshots.Count.ShouldBe(1);
        session.Snapshots[0].Name.ShouldBe("First Snapshot");
    }
}
