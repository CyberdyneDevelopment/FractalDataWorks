using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Internal state tracking for a managed session.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
internal sealed class ManagedSessionState
{
    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the path to the solution file.
    /// </summary>
    public required string SolutionPath { get; set; }

    /// <summary>
    /// Gets or sets the human-readable description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the conversation ID.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets when the session was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets when the session was last modified.
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets when the session was last accessed.
    /// </summary>
    public DateTimeOffset LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the underlying workspace, or null if sleeping.
    /// </summary>
    public IRoslynWorkspace? Workspace { get; set; }

    /// <summary>
    /// Gets or sets whether the workspace is sleeping.
    /// </summary>
    public bool IsSleeping { get; set; }

    /// <summary>
    /// Gets or sets whether the session has unsaved changes.
    /// </summary>
    public bool HasPendingChanges { get; set; }

    /// <summary>
    /// Gets or sets the list of snapshots for this session.
    /// </summary>
    public List<SessionSnapshot> Snapshots { get; set; } = [];

    /// <summary>
    /// Gets or sets the document changes since last baseline.
    /// </summary>
    public Dictionary<string, string> DocumentChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the baseline commit hash when the session was created.
    /// </summary>
    public string? BaselineCommitHash { get; set; }

    /// <summary>
    /// Gets or sets the baseline timestamp when the session was created.
    /// </summary>
    public DateTimeOffset? BaselineTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the patterns used to exclude projects from loading.
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = [];

    /// <summary>
    /// Converts the state to public session info.
    /// </summary>
    /// <returns>Public session information.</returns>
    public SessionInfo ToSessionInfo()
    {
        var loadedProjects = Workspace?.GetLoadedProjects();
        var allProjects = Workspace?.GetAllProjects();

        return new SessionInfo
        {
            Id = Id,
            SolutionPath = SolutionPath,
            Description = Description,
            CreatedAt = CreatedAt,
            LastModifiedAt = LastModifiedAt,
            ConversationId = ConversationId,
            ProjectCount = loadedProjects?.Count ?? Workspace?.CurrentSolution?.Projects.Count() ?? 0,
            TotalProjectCount = allProjects?.Count ?? Workspace?.CurrentSolution?.Projects.Count() ?? 0,
            ExcludedProjectCount = (allProjects?.Count ?? 0) - (loadedProjects?.Count ?? 0),
            SnapshotCount = Snapshots.Count,
            HasPendingChanges = HasPendingChanges,
            IsActive = true,
            IsSleeping = IsSleeping,
            ExcludePatterns = Workspace?.ExcludePatterns?.ToList() ?? ExcludePatterns
        };
    }
}