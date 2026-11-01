using System;
using System.Threading.Tasks;
using Fdw.Workspace.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Workspace.Management.Tests;

[Collection(nameof(WorkspaceTestCollection))]
public class WorkspaceManagerTests : IDisposable
{
    private readonly InMemorySessionStore _sessionStore = new();
    private readonly ILogger<WorkspaceManager> _logger = NullLoggerFactory.Instance.CreateLogger<WorkspaceManager>();
    private readonly WorkspaceManager _sut;

    public WorkspaceManagerTests()
    {
        _sut = new WorkspaceManager(_sessionStore, null, _logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullSessionStoreThrows()
    {
        Should.Throw<ArgumentNullException>(() => new WorkspaceManager(null!));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void InitialWorkspaceCountIsZero()
    {
        _sut.WorkspaceCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void IsLoadedReturnsFalseForUnknownId()
    {
        _sut.IsLoaded(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ListWorkspacesReturnsEmptyInitially()
    {
        _sut.ListWorkspaces().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ListSessionsDelegatesToSessionStore()
    {
        var sessions = await _sut.ListSessions(TestContext.Current.CancellationToken);

        sessions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task GetWorkspaceWithUnknownIdReturnsFailure()
    {
        var result = await _sut.GetWorkspace(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task UnloadWorkspaceWithUnknownIdReturnsFailure()
    {
        var result = await _sut.UnloadWorkspace(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadWorkspaceWithNullPathReturnsFailure()
    {
        var result = await _sut.LoadWorkspace(null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadWorkspaceWithEmptyPathReturnsFailure()
    {
        var result = await _sut.LoadWorkspace("", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadWorkspaceWithWhitespacePathReturnsFailure()
    {
        var result = await _sut.LoadWorkspace("   ", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task LoadWorkspaceWithNonExistentFileReturnsFailure()
    {
        var result = await _sut.LoadWorkspace("/nonexistent/path/solution.sln", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SaveSessionWithUnknownWorkspaceReturnsFailure()
    {
        var result = await _sut.SaveSession(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ResumeSessionWithUnknownSessionReturnsFailure()
    {
        var result = await _sut.ResumeSession(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DoubleDisposeDoesNotThrow()
    {
        _sut.Dispose();
        Should.NotThrow(() => _sut.Dispose());
    }
}
