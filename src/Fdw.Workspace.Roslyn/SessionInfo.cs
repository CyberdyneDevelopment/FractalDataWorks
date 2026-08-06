#pragma warning disable MA0016 // Prefer using collection abstraction - DTOs need concrete types for serialization

using System;
using System.Collections.Generic;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents information about a managed session.
/// </summary>
/// <remarks>
/// <para>
/// A session wraps a workspace and provides additional tracking for:
/// conversation continuity, description, and persistence. Multiple sessions
/// can work against the same solution with isolated workspaces.
/// </para>
/// </remarks>
public sealed record SessionInfo
{
    /// <summary>
    /// Gets the unique identifier for the session.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the path to the solution file.
    /// </summary>
    public required string SolutionPath { get; init; }

    /// <summary>
    /// Gets the human-readable description of the session.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets when the session was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets when the session was last modified.
    /// </summary>
    public required DateTimeOffset LastModifiedAt { get; init; }

    /// <summary>
    /// Gets the conversation ID for Claude resume capability.
    /// </summary>
    /// <remarks>
    /// When set, this allows Claude to identify which session belongs to
    /// which conversation when resuming work across multiple sessions.
    /// </remarks>
    public string? ConversationId { get; init; }

    /// <summary>
    /// Gets the number of projects loaded in the session's solution.
    /// </summary>
    public int ProjectCount { get; init; }

    /// <summary>
    /// Gets the total number of projects in the solution (including excluded).
    /// </summary>
    public int TotalProjectCount { get; init; }

    /// <summary>
    /// Gets the number of projects excluded from loading.
    /// </summary>
    public int ExcludedProjectCount { get; init; }

    /// <summary>
    /// Gets the number of snapshots taken in this session.
    /// </summary>
    public int SnapshotCount { get; init; }

    /// <summary>
    /// Gets whether the session has unsaved changes.
    /// </summary>
    public bool HasPendingChanges { get; init; }

    /// <summary>
    /// Gets whether the session is currently active (loaded in memory).
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets whether the session's workspace is sleeping to conserve memory.
    /// </summary>
    public bool IsSleeping { get; init; }

    /// <summary>
    /// Gets the patterns used to exclude projects from loading.
    /// </summary>
    public List<string> ExcludePatterns { get; init; } = [];
}