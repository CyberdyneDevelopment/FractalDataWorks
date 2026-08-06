using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for idle eviction — the path that used to destroy work and report success.
/// </summary>
/// <remarks>
/// Both behaviours here were found by controlled experiment against a live server rather than by the
/// suite, because both are invisible in the returned result: an eviction between a refactor and its
/// commit produced two successes and no effect, and three concurrent calls produced three workspaces
/// where the API promises one.
/// </remarks>
public sealed class WorkspaceEvictionTests
{
    private const string SolutionPath = "/repo/Fake.slnx";

    private static Mock<IRoslynWorkspace> NewWorkspace(IReadOnlyDictionary<string, string>? pending = null)
    {
        var workspace = new Mock<IRoslynWorkspace>();
        workspace.SetupGet(w => w.CurrentSolution).Returns(new AdhocWorkspace().CurrentSolution);
        workspace.Setup(w => w.GetChangesFromBaseline())
            .Returns(pending ?? new Dictionary<string, string>(StringComparer.Ordinal));
        return workspace;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AWorkspaceHoldingUncommittedChangesIsNotEvicted()
    {
        // The observed failure: MoveNamespace, then an idle tick, then ApplyWorkspaceChanges returned
        // success: true / "Wrote 0 file(s) to disk" and the disk was unchanged. Eviction had discarded
        // the in-memory solution, so the commit correctly found nothing to do. Reclaiming memory is
        // never a reason to throw away work that exists nowhere else.
        var factory = new Mock<IRoslynWorkspaceFactory>();
        factory.Setup(f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewWorkspace(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["/repo/Moved.cs"] = "namespace New;",
            }).Object);

        using var manager = new WorkspaceManager(factory.Object);
        var (id, _) = await manager.OpenSolution(SolutionPath, setAsActive: true, TestContext.Current.CancellationToken);

        manager.SleepWorkspace(id).ShouldBeFalse("a workspace with pending changes must stay resident");
        manager.ListWorkspaces().Single(w => string.Equals(w.Id, id, StringComparison.Ordinal))
            .IsActive.ShouldBeTrue("still loaded, so the pending edit survives to be committed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AWorkspaceWithNothingPendingIsStillEvicted()
    {
        // The refusal must be narrow. If it also kept idle, clean workspaces resident it would be a
        // memory leak wearing a correctness fix as a disguise.
        var factory = new Mock<IRoslynWorkspaceFactory>();
        factory.Setup(f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => NewWorkspace().Object);

        using var manager = new WorkspaceManager(factory.Object);
        var (id, _) = await manager.OpenSolution(SolutionPath, setAsActive: true, TestContext.Current.CancellationToken);

        manager.SleepWorkspace(id).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ConcurrentCallsOnASleepingWorkspaceWakeItExactlyOnce()
    {
        // Proved against the live server: one eviction plus three concurrent calls produced three
        // MSBuild reloads and three IRoslynWorkspace instances for one id. Two were orphaned along with
        // anything their callers had already been given — the API hands out one workspace per id, and
        // silently returning three different objects makes every guarantee above it meaningless.
        var created = 0;
        var factory = new Mock<IRoslynWorkspaceFactory>();
        factory.Setup(f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref created);
                // A real wake loads a solution from disk; the delay is what opens the race window.
                await Task.Delay(50, TestContext.Current.CancellationToken);
                return NewWorkspace().Object;
            });

        using var manager = new WorkspaceManager(factory.Object);
        var (id, _) = await manager.OpenSolution(SolutionPath, setAsActive: true, TestContext.Current.CancellationToken);

        created.ShouldBe(1, "the initial open");
        manager.SleepWorkspace(id).ShouldBeTrue();

        var woken = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            manager.GetWorkspace(id, TestContext.Current.CancellationToken)));

        created.ShouldBe(2, "one open plus exactly one wake, however many callers raced");
        woken.Distinct().Count().ShouldBe(1, "every caller must receive the same instance");
    }
}
