using Fdw.Workspace.Management;

namespace Fdw.Workspace.Management.Tests;

[Collection(nameof(WorkspaceTestCollection))]
public class FileBasedSessionStoreTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileBasedSessionStore _store;

    public FileBasedSessionStoreTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FDW_Test_{Guid.NewGuid():N}");
        _store = new FileBasedSessionStore(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Save_CreatesJsonFile()
    {
        // Arrange
        var session = CreateTestSession();

        // Act
        var result = await _store.Save(session, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var expectedPath = Path.Combine(_testDirectory, $"{session.Id}.session.json");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Load_ReadsJsonFile()
    {
        // Arrange
        var session = CreateTestSession();
        session.Name = "TestSolution";
        session.SolutionPath = @"C:\Projects\Test.sln";
        await _store.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await _store.Load(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(session.Id);
        result.Value.Name.ShouldBe("TestSolution");
        result.Value.SolutionPath.ShouldBe(@"C:\Projects\Test.sln");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Load_PreservesSnapshots()
    {
        // Arrange
        var session = CreateTestSession();
        session.Snapshots.Add(new SnapshotRecord
        {
            Id = "snap-1",
            Name = "Initial",
            Description = "First snapshot",
            CreatedAt = DateTimeOffset.UtcNow
        });
        session.Snapshots[0].DocumentChanges["Program.cs"] = "// modified";
        await _store.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await _store.Load(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Snapshots.Count.ShouldBe(1);
        result.Value.Snapshots[0].Name.ShouldBe("Initial");
        result.Value.Snapshots[0].DocumentChanges["Program.cs"].ShouldBe("// modified");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Delete_RemovesFile()
    {
        // Arrange
        var session = CreateTestSession();
        await _store.Save(session, TestContext.Current.CancellationToken);
        var filePath = Path.Combine(_testDirectory, $"{session.Id}.session.json");
        File.Exists(filePath).ShouldBeTrue();

        // Act
        var result = await _store.Delete(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task List_ReturnsAllSessions()
    {
        // Arrange
        var session1 = CreateTestSession();
        var session2 = CreateTestSession();
        await _store.Save(session1, TestContext.Current.CancellationToken);
        await _store.Save(session2, TestContext.Current.CancellationToken);

        // Act
        var sessions = await _store.List(TestContext.Current.CancellationToken);

        // Assert
        sessions.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Exists_ReturnsTrue_WhenFileExists()
    {
        // Arrange
        var session = CreateTestSession();
        await _store.Save(session, TestContext.Current.CancellationToken);

        // Act
        var exists = await _store.Exists(session.Id, TestContext.Current.CancellationToken);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Exists_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Act
        var exists = await _store.Exists(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        exists.ShouldBeFalse();
    }

    private static WorkspaceSession CreateTestSession()
    {
        return new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = @"C:\Test\Solution.sln",
            Name = "TestSolution",
            CreatedAt = DateTimeOffset.UtcNow,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1
        };
    }
}
