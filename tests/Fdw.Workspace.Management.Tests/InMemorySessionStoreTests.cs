using Fdw.Workspace.Management;

namespace Fdw.Workspace.Management.Tests;

[Collection(nameof(WorkspaceTestCollection))]
public class InMemorySessionStoreTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Save_StoresSession()
    {
        // Arrange
        var store = new InMemorySessionStore();
        var session = CreateTestSession();

        // Act
        var result = await store.Save(session, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        store.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Load_ReturnsSession_WhenExists()
    {
        // Arrange
        var store = new InMemorySessionStore();
        var session = CreateTestSession();
        await store.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await store.Load(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(session.Id);
        result.Value.SolutionPath.ShouldBe(session.SolutionPath);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Load_ReturnsFailure_WhenNotExists()
    {
        // Arrange
        var store = new InMemorySessionStore();

        // Act
        var result = await store.Load(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Delete_RemovesSession()
    {
        // Arrange
        var store = new InMemorySessionStore();
        var session = CreateTestSession();
        await store.Save(session, TestContext.Current.CancellationToken);

        // Act
        var result = await store.Delete(session.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        store.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task List_ReturnsAllSessions()
    {
        // Arrange
        var store = new InMemorySessionStore();
        var session1 = CreateTestSession();
        var session2 = CreateTestSession();
        await store.Save(session1, TestContext.Current.CancellationToken);
        await store.Save(session2, TestContext.Current.CancellationToken);

        // Act
        var sessions = await store.List(TestContext.Current.CancellationToken);

        // Assert
        sessions.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Exists_ReturnsTrue_WhenSessionExists()
    {
        // Arrange
        var store = new InMemorySessionStore();
        var session = CreateTestSession();
        await store.Save(session, TestContext.Current.CancellationToken);

        // Act
        var exists = await store.Exists(session.Id, TestContext.Current.CancellationToken);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Exists_ReturnsFalse_WhenSessionDoesNotExist()
    {
        // Arrange
        var store = new InMemorySessionStore();

        // Act
        var exists = await store.Exists(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Clear_RemovesAllSessions()
    {
        // Arrange
        var store = new InMemorySessionStore();
        await store.Save(CreateTestSession(), TestContext.Current.CancellationToken);
        await store.Save(CreateTestSession(), TestContext.Current.CancellationToken);

        // Act
        store.Clear();

        // Assert
        store.Count.ShouldBe(0);
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
