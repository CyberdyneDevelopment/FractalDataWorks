using Fdw.Results;
using Fdw.Workspace.Management;
using Fdw.Workspace.Roslyn;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Workspace.Management.Tests;

[Collection(nameof(WorkspaceTestCollection))]
public class WorkspaceManagerAdditionalTests : IDisposable
{
    private readonly InMemorySessionStore _sessionStore = new();
    private readonly Mock<IRoslynWorkspaceFactory> _factoryMock = new();
    private readonly ILogger<WorkspaceManager> _logger = NullLoggerFactory.Instance.CreateLogger<WorkspaceManager>();
    private readonly WorkspaceManager _sut;
    private readonly string _tempSolutionPath;

    public WorkspaceManagerAdditionalTests()
    {
        // Create a temp file to act as a solution path that exists on disk
        _tempSolutionPath = Path.Combine(Path.GetTempPath(), $"FDW_Test_{Guid.NewGuid():N}.sln");
        File.WriteAllText(_tempSolutionPath, "");

        _sut = new WorkspaceManager(_sessionStore, _factoryMock.Object, _logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_tempSolutionPath))
        {
            File.Delete(_tempSolutionPath);
        }
    }

    private Mock<IRoslynWorkspace> CreateMockWorkspace(int projectCount = 3, bool hasChanges = false, int snapshotCount = 0)
    {
        var workspaceMock = new Mock<IRoslynWorkspace>();

        // Mock CurrentSolution with ProjectIds
        var solutionMock = new Mock<Solution>();

        // Use AdhocWorkspace to get a real Solution with projects
        var adhoc = new AdhocWorkspace();
        var solution = adhoc.CurrentSolution;
        for (int i = 0; i < projectCount; i++)
        {
            var projectInfo = Microsoft.CodeAnalysis.ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                $"Project{i}",
                $"Project{i}",
                LanguageNames.CSharp);
            solution = solution.AddProject(projectInfo);
        }

        workspaceMock.Setup(w => w.CurrentSolution).Returns(solution);
        workspaceMock.Setup(w => w.HasChanges).Returns(hasChanges);
        workspaceMock.Setup(w => w.SnapshotCount).Returns(snapshotCount);
        workspaceMock.Setup(w => w.Baseline).Returns((Solution?)null);
        workspaceMock.Setup(w => w.ListSnapshots()).Returns([]);
        workspaceMock.Setup(w => w.GetChangesFromBaseline()).Returns(new Dictionary<string, string>());

        adhoc.Dispose();
        return workspaceMock;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadWorkspaceReturnsSuccessWhenSolutionFileExists()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        // Act
        var result = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        _sut.WorkspaceCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadWorkspaceIncrementsWorkspaceCount()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        // Act
        var result = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Assert
        _sut.WorkspaceCount.ShouldBe(1);
        _sut.IsLoaded(result.Value).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task GetWorkspaceReturnsWorkspaceAfterLoad()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var loadResult = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var getResult = await _sut.GetWorkspace(loadResult.Value, TestContext.Current.CancellationToken);

        // Assert
        getResult.IsSuccess.ShouldBeTrue();
        getResult.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task UnloadWorkspaceReturnsSuccessAndDecrementsCount()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        workspaceMock.As<IDisposable>().Setup(d => d.Dispose());
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var loadResult = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var unloadResult = await _sut.UnloadWorkspace(loadResult.Value, TestContext.Current.CancellationToken);

        // Assert
        unloadResult.IsSuccess.ShouldBeTrue();
        _sut.WorkspaceCount.ShouldBe(0);
        _sut.IsLoaded(loadResult.Value).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListWorkspacesReturnsLoadedWorkspaces()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace(projectCount: 2, hasChanges: true, snapshotCount: 1);
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var workspaces = _sut.ListWorkspaces().ToList();

        // Assert
        workspaces.Count.ShouldBe(1);
        workspaces[0].ProjectCount.ShouldBe(2);
        workspaces[0].HasChanges.ShouldBeTrue();
        workspaces[0].SnapshotCount.ShouldBe(1);
        workspaces[0].SolutionPath.ShouldBe(_tempSolutionPath);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadWorkspaceReturnsFailureWhenFactoryThrows()
    {
        // Arrange
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Factory failure"));

        // Act
        var result = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SaveSessionReturnsSuccessForLoadedWorkspace()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        workspaceMock.Setup(w => w.Baseline).Returns((Solution?)null);
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var loadResult = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var saveResult = await _sut.SaveSession(loadResult.Value, TestContext.Current.CancellationToken);

        // Assert
        saveResult.IsSuccess.ShouldBeTrue();
        saveResult.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SaveSessionCapturesSnapshotsFromWorkspace()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        var snapshotChanges = new Dictionary<string, string>
        {
            ["file1.cs"] = "// snapshot content"
        };
        workspaceMock.Setup(w => w.ListSnapshots()).Returns(
        [
            new SnapshotInfo("snap-1", "Snapshot 1", "First", DateTime.UtcNow),
            new SnapshotInfo("snap-2", "Snapshot 2", "Second", DateTime.UtcNow)
        ]);
        workspaceMock.Setup(w => w.GetChangesFromSnapshot("snap-1")).Returns(snapshotChanges);
        workspaceMock.Setup(w => w.GetChangesFromSnapshot("snap-2")).Returns(snapshotChanges);
        workspaceMock.Setup(w => w.GetChangesFromBaseline()).Returns(new Dictionary<string, string>());
        workspaceMock.Setup(w => w.Baseline).Returns((Solution?)null);

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var loadResult = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var saveResult = await _sut.SaveSession(loadResult.Value, TestContext.Current.CancellationToken);

        // Assert
        saveResult.IsSuccess.ShouldBeTrue();

        // Verify session was saved to store
        var sessionExists = await _sessionStore.Exists(saveResult.Value, TestContext.Current.CancellationToken);
        sessionExists.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SaveSessionIncludesBaselineChanges()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        var baselineChanges = new Dictionary<string, string>
        {
            ["baseFile.cs"] = "// baseline content"
        };
        workspaceMock.Setup(w => w.GetChangesFromBaseline()).Returns(baselineChanges);

        // Need a non-null baseline to trigger the BaselineSnapshot property
        var adhoc = new AdhocWorkspace();
        workspaceMock.Setup(w => w.Baseline).Returns(adhoc.CurrentSolution);
        workspaceMock.Setup(w => w.ListSnapshots()).Returns([]);

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var loadResult = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var saveResult = await _sut.SaveSession(loadResult.Value, TestContext.Current.CancellationToken);

        // Assert
        saveResult.IsSuccess.ShouldBeTrue();

        // Load session and verify baseline was included
        var sessionResult = await _sessionStore.Load(saveResult.Value, TestContext.Current.CancellationToken);
        sessionResult.IsSuccess.ShouldBeTrue();
        sessionResult.Value!.BaselineSnapshot.ShouldBe("baseline");
        sessionResult.Value.Snapshots.ShouldContain(s => string.Equals(s.Id, "baseline", StringComparison.Ordinal));

        adhoc.Dispose();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SaveSessionSkipsSnapshotsWithNullChanges()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        workspaceMock.Setup(w => w.ListSnapshots()).Returns(
        [
            new SnapshotInfo("snap-1", "Snapshot 1", "First", DateTime.UtcNow),
        ]);
        // Return null for this snapshot
        workspaceMock.Setup(w => w.GetChangesFromSnapshot("snap-1")).Returns((IReadOnlyDictionary<string, string>?)null);
        workspaceMock.Setup(w => w.GetChangesFromBaseline()).Returns(new Dictionary<string, string>());
        workspaceMock.Setup(w => w.Baseline).Returns((Solution?)null);

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var loadResult = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var saveResult = await _sut.SaveSession(loadResult.Value, TestContext.Current.CancellationToken);

        // Assert
        saveResult.IsSuccess.ShouldBeTrue();

        var sessionResult = await _sessionStore.Load(saveResult.Value, TestContext.Current.CancellationToken);
        sessionResult.IsSuccess.ShouldBeTrue();
        // Snapshot with null changes should be skipped
        sessionResult.Value!.Snapshots.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ResumeSessionWithNonExistentSolutionReturnsFailure()
    {
        // Arrange - Create a session pointing to a non-existent solution
        var session = new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = "/nonexistent/path/solution.sln",
            Name = "NonExistent",
            CreatedAt = DateTimeOffset.UtcNow,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1
        };
        await _sessionStore.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.ResumeSession(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ResumeSessionLoadsWorkspaceAndAppliesBaseline()
    {
        // Arrange - Create a session with baseline changes
        var workspaceMock = CreateMockWorkspace();
        workspaceMock.Setup(w => w.ApplyDocumentChanges(It.IsAny<IReadOnlyDictionary<string, string>>()));
        workspaceMock.Setup(w => w.CreateSnapshot(It.IsAny<string>(), It.IsAny<string>())).Returns("new-snap-id");

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var session = new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = _tempSolutionPath,
            Name = Path.GetFileNameWithoutExtension(_tempSolutionPath),
            CreatedAt = DateTimeOffset.UtcNow,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1,
            BaselineSnapshot = "baseline",
            Snapshots =
            [
                new SnapshotRecord
                {
                    Id = "baseline",
                    Name = "Baseline",
                    Description = "Changes from disk state",
                    CreatedAt = DateTimeOffset.UtcNow,
                    DocumentChanges = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["file1.cs"] = "// baseline content"
                    }
                },
                new SnapshotRecord
                {
                    Id = "snap-1",
                    Name = "User Snapshot",
                    Description = "A saved snapshot",
                    CreatedAt = DateTimeOffset.UtcNow,
                    DocumentChanges = new Dictionary<string, string>(StringComparer.Ordinal)
                }
            ]
        };
        await _sessionStore.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.ResumeSession(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        // Verify baseline changes were applied
        workspaceMock.Verify(w => w.ApplyDocumentChanges(It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);

        // Verify non-baseline snapshot was recreated
        workspaceMock.Verify(w => w.CreateSnapshot("User Snapshot", "A saved snapshot"), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ResumeSessionWithEmptyBaselineDoesNotApplyChanges()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var session = new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = _tempSolutionPath,
            Name = Path.GetFileNameWithoutExtension(_tempSolutionPath),
            CreatedAt = DateTimeOffset.UtcNow,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1,
            BaselineSnapshot = "baseline",
            Snapshots =
            [
                new SnapshotRecord
                {
                    Id = "baseline",
                    Name = "Baseline",
                    Description = "Changes from disk state",
                    CreatedAt = DateTimeOffset.UtcNow,
                    DocumentChanges = new Dictionary<string, string>(StringComparer.Ordinal) // empty
                }
            ]
        };
        await _sessionStore.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.ResumeSession(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Empty baseline changes should not call ApplyDocumentChanges
        workspaceMock.Verify(w => w.ApplyDocumentChanges(It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ResumeSessionWithNoBaselineRecordSkipsBaselineApply()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var session = new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = _tempSolutionPath,
            Name = Path.GetFileNameWithoutExtension(_tempSolutionPath),
            CreatedAt = DateTimeOffset.UtcNow,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1,
            // No baseline
            Snapshots = []
        };
        await _sessionStore.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.ResumeSession(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        workspaceMock.Verify(w => w.ApplyDocumentChanges(It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task DisposeDisposesAllLoadedWorkspaces()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        workspaceMock.As<IDisposable>().Setup(d => d.Dispose());

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        _sut.Dispose();

        // Assert
        workspaceMock.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
        _sut.WorkspaceCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListSessionsDelegatesToStore()
    {
        // Arrange - Save a session directly to the store
        var session = new WorkspaceSession
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            SolutionPath = "/some/path.sln",
            Name = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1
        };
        await _sessionStore.Save(session, TestContext.Current.CancellationToken);

        // Act
        var sessions = await _sut.ListSessions(TestContext.Current.CancellationToken);

        // Assert
        sessions.Count().ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ToStringOnInactiveScheduleShowsInactive()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace(hasChanges: false);
        workspaceMock.Setup(w => w.Baseline).Returns((Solution?)null);
        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        var loadResult = await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var workspaces = _sut.ListWorkspaces().ToList();

        // Assert
        workspaces[0].HasBaseline.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListWorkspacesReturnsWorkspacesWithBaseline()
    {
        // Arrange
        var workspaceMock = CreateMockWorkspace();
        var adhoc = new AdhocWorkspace();
        workspaceMock.Setup(w => w.Baseline).Returns(adhoc.CurrentSolution);

        _factoryMock.Setup(f => f.CreateFromSolution(_tempSolutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaceMock.Object);

        await _sut.LoadWorkspace(_tempSolutionPath, TestContext.Current.CancellationToken);

        // Act
        var workspaces = _sut.ListWorkspaces().ToList();

        // Assert
        workspaces[0].HasBaseline.ShouldBeTrue();
        adhoc.Dispose();
    }
}
