using Fdw.Workspace.Management;

namespace Fdw.Workspace.Management.Tests;

[Collection(nameof(WorkspaceTestCollection))]
public class FileBasedSessionStoreAdditionalTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileBasedSessionStore _store;

    public FileBasedSessionStoreAdditionalTests()
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
    public async Task LoadReturnsFailureWhenSessionFileDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _store.Load(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadReturnsFailureWhenJsonIsInvalid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = Path.Combine(_testDirectory, $"{sessionId}.session.json");
        File.WriteAllText(filePath, "not valid json {{{");

        // Act
        var result = await _store.Load(sessionId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadReturnsFailureWhenJsonDeserializesToNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filePath = Path.Combine(_testDirectory, $"{sessionId}.session.json");
        File.WriteAllText(filePath, "null");

        // Act
        var result = await _store.Load(sessionId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteReturnsSuccessWhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _store.Delete(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListReturnsEmptyWhenDirectoryIsEmpty()
    {
        // Act
        var sessions = await _store.List(TestContext.Current.CancellationToken);

        // Assert
        sessions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListSkipsCorruptedSessionFiles()
    {
        // Arrange - save one valid session
        var validSession = CreateTestSession();
        await _store.Save(validSession, TestContext.Current.CancellationToken);

        // Add a corrupted session file
        var corruptId = Guid.NewGuid();
        var corruptPath = Path.Combine(_testDirectory, $"{corruptId}.session.json");
        File.WriteAllText(corruptPath, "corrupted json {{{");

        // Act
        var sessions = await _store.List(TestContext.Current.CancellationToken);

        // Assert - should still return the valid session, skipping the corrupted one
        sessions.Count().ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListReturnsSessionInfoWithCorrectProperties()
    {
        // Arrange
        var session = CreateTestSession();
        session.Name = "TestSolution";
        session.SolutionPath = "/test/solution.sln";
        session.BaselineSnapshot = "baseline";
        session.Snapshots.Add(new SnapshotRecord
        {
            Id = "snap-1",
            Name = "Snap1",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _store.Save(session, TestContext.Current.CancellationToken);

        // Act
        var sessions = (await _store.List(TestContext.Current.CancellationToken)).ToList();

        // Assert
        sessions.Count.ShouldBe(1);
        var info = sessions[0];
        info.Id.ShouldBe(session.Id);
        info.Name.ShouldBe("TestSolution");
        info.SolutionPath.ShouldBe("/test/solution.sln");
        info.SnapshotCount.ShouldBe(1);
        info.HasBaseline.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListReturnsSessionsOrderedByDescendingSavedAt()
    {
        // Arrange
        var session1 = CreateTestSession();
        session1.SavedAt = DateTimeOffset.UtcNow.AddHours(-2);
        session1.Name = "Older";
        await _store.Save(session1, TestContext.Current.CancellationToken);

        var session2 = CreateTestSession();
        session2.SavedAt = DateTimeOffset.UtcNow;
        session2.Name = "Newer";
        await _store.Save(session2, TestContext.Current.CancellationToken);

        // Act
        var sessions = (await _store.List(TestContext.Current.CancellationToken)).ToList();

        // Assert
        sessions.Count.ShouldBe(2);
        sessions[0].Name.ShouldBe("Newer");
        sessions[1].Name.ShouldBe("Older");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullDirectoryUsesDefault()
    {
        // Act - should not throw
        var store = new FileBasedSessionStore(sessionDirectory: null);

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorCreatesDirectoryIfNotExists()
    {
        // Arrange
        var newDir = Path.Combine(Path.GetTempPath(), $"FDW_NewDir_{Guid.NewGuid():N}");

        try
        {
            // Act
            var store = new FileBasedSessionStore(newDir);

            // Assert
            Directory.Exists(newDir).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(newDir))
            {
                Directory.Delete(newDir, true);
            }
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListReturnsEmptyWhenDirectoryDoesNotExist()
    {
        // Arrange - create store with non-existent dir, then remove it
        var tempDir = Path.Combine(Path.GetTempPath(), $"FDW_NoDir_{Guid.NewGuid():N}");
        var store = new FileBasedSessionStore(tempDir);
        Directory.Delete(tempDir, true);

        // Act
        var sessions = await store.List(TestContext.Current.CancellationToken);

        // Assert
        sessions.ShouldBeEmpty();
    }

    private static WorkspaceSession CreateTestSession()
    {
        return new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = "/test/solution.sln",
            Name = "TestSolution",
            CreatedAt = DateTimeOffset.UtcNow,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1
        };
    }
}
