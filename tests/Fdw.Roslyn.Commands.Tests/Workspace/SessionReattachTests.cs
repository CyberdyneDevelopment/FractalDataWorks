using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Workspace.Roslyn;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Roslyn.Commands.Tests.Workspace;

/// <summary>
/// Tests that an agent reattaches to its own session across a process boundary.
/// </summary>
/// <remarks>
/// <para>
/// A second <see cref="SessionManager"/> over the same store stands in for a restarted process:
/// nothing is shared but the store on disk, which is exactly the situation a reconnecting MCP client
/// creates. Testing it within one manager would prove nothing, because the in-memory dictionary
/// answers every lookup and the persisted path — the one that was broken — never runs.
/// </para>
/// <para>
/// The store is seeded directly rather than through <c>CreateSession</c>. CreateSession performs a
/// real MSBuild solution load, which needs MSBuildLocator registered in the test host and takes
/// seconds per case; seeding isolates the lookup behaviour these tests are about. The consequence is
/// stated plainly: persistence-on-create is exercised by the load path, not by this file.
/// </para>
/// </remarks>
public sealed class SessionReattachTests : IDisposable
{
    // Why a temp subdirectory and not a fixed folder under the temp path: a shared
    // "fdw-session-reattach-tests" segment is created by whoever runs the suite first and is
    // owned by them, so on a machine where CI and a developer both run, the second one cannot
    // write into it at all.
    private readonly string _storePath = Directory.CreateTempSubdirectory("fdw-session-reattach-tests-").FullName;

    private FileBasedSessionStore NewStore() =>
        new(NullLogger<FileBasedSessionStore>.Instance, _storePath);

    private static SessionManager NewManager(FileBasedSessionStore store) =>
        new(new RoslynWorkspaceFactory(), store, store, logger: null, sleepTimeout: TimeSpan.MaxValue);

    private static PersistedSession NewPersisted(Guid id, string conversationId) => new()
    {
        Id = id,
        SolutionPath = "/does/not/need/to/exist.slnx",
        Description = "reattach test",
        ConversationId = conversationId,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task FindByConversationIdLocatesASessionPersistedByAnEarlierProcess()
    {
        var seedStore = NewStore();
        seedStore.EnsureStoreExists().IsSuccess.ShouldBeTrue();

        var originalId = Guid.NewGuid();
        (await seedStore.SaveSession(NewPersisted(originalId, "agent-alpha"), TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();

        // A brand new manager over the same store — the stand-in for a reconnecting process.
        using var reconnected = NewManager(NewStore());

        reconnected.FindSessionByConversationId("agent-alpha")
            .ShouldBeNull("the sync overload only sees sessions this process created");

        var found = await reconnected.FindSessionByConversationId("agent-alpha", TestContext.Current.CancellationToken);

        found.ShouldNotBeNull("a reconnecting agent must find the session it already had");
        found!.Id.ShouldBe(originalId);
    }

    [Fact]
    public async Task FindByConversationIdReturnsNullForAnUnknownAgent()
    {
        var seedStore = NewStore();
        seedStore.EnsureStoreExists().IsSuccess.ShouldBeTrue();
        (await seedStore.SaveSession(NewPersisted(Guid.NewGuid(), "agent-alpha"), TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();

        using var manager = NewManager(NewStore());

        (await manager.FindSessionByConversationId("agent-beta", TestContext.Current.CancellationToken))
            .ShouldBeNull("a different agent must get its own session, not someone else's");
    }

    [Fact]
    public async Task FindByConversationIdReturnsNullWhenTheConversationIdIsEmpty()
    {
        using var manager = NewManager(NewStore());

        // Why this case matters: an empty id must never match, or every caller that omitted a
        // conversation id would collide onto one shared session.
        (await manager.FindSessionByConversationId(string.Empty, TestContext.Current.CancellationToken))
            .ShouldBeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_storePath))
            Directory.Delete(_storePath, recursive: true);
    }
}
