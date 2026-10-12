using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Internal state for a managed workspace.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
internal sealed class ManagedWorkspaceState
{
    public required string Id { get; init; }
    public required string SolutionPath { get; init; }
    public IRoslynWorkspace? Workspace { get; set; }
    public int ProjectCount { get; set; }
    public DateTime LoadedAt { get; init; }
    public DateTime LastAccessedAt { get; set; }
    public bool IsSleeping => Workspace is null;

    /// <summary>
    /// Serializes every transition of <see cref="Workspace"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Workspace"/> was assigned unsynchronized from the Timer thread and from every wake
    /// path at once. Three concurrent calls against one sleeping workspace produced three MSBuild
    /// reloads and three live <see cref="IRoslynWorkspace"/> instances for a single id — two of them
    /// orphaned, along with whatever a caller had already done to them.
    ///
    /// A SemaphoreSlim rather than a lock because waking is asynchronous: it loads a solution from disk,
    /// so the exclusion has to survive an await, which <c>lock</c> cannot do. Never disposed, and it does
    /// not need to be — the wait handle is allocated lazily on first access to AvailableWaitHandle, which
    /// nothing here touches.
    /// </remarks>
    public SemaphoreSlim Gate { get; } = new(1, 1);
}