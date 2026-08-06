#pragma warning disable MA0016 // Prefer using collection abstraction - DTOs need concrete types for JSON serialization

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents a session persisted to the system store.
/// </summary>
/// <remarks>
/// <para>
/// Stored at: ~/.local/share/roslyn-mcp/sessions/{id}.json (Linux)
/// or %LOCALAPPDATA%/roslyn-mcp/sessions/{id}.json (Windows).
/// </para>
/// <para>
/// Contains full session data including document changes and snapshots.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class PersistedSession
{
    /// <summary>
    /// Gets or sets the schema version for forward compatibility.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the path to the solution file.
    /// </summary>
    [JsonPropertyName("solutionPath")]
    public string SolutionPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the session description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>
    /// Gets or sets the conversation ID for Claude resume.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets when the session was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the session was last modified.
    /// </summary>
    [JsonPropertyName("lastModifiedAt")]
    public DateTimeOffset LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the baseline information (commit hash/timestamp).
    /// </summary>
    [JsonPropertyName("baseline")]
    public SessionBaseline? Baseline { get; set; }

    /// <summary>
    /// Gets or sets document changes keyed by file path.
    /// </summary>
    [JsonPropertyName("documentChanges")]
    public Dictionary<string, string> DocumentChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of snapshots.
    /// </summary>
    [JsonPropertyName("snapshots")]
    public List<PersistedSnapshot> Snapshots { get; set; } = [];

    /// <summary>
    /// Converts to public SessionInfo.
    /// </summary>
    /// <returns>Session information.</returns>
    public SessionInfo ToSessionInfo() => new()
    {
        Id = Id,
        SolutionPath = SolutionPath,
        Description = Description,
        CreatedAt = CreatedAt,
        LastModifiedAt = LastModifiedAt,
        ConversationId = ConversationId,
        ProjectCount = 0, // Not known until loaded
        SnapshotCount = Snapshots.Count,
        HasPendingChanges = DocumentChanges.Count > 0,
        IsActive = false,
        IsSleeping = false
    };

    /// <summary>
    /// Creates from managed state.
    /// </summary>
    /// <param name="state">The managed session state.</param>
    /// <returns>A persisted session.</returns>
    internal static PersistedSession FromState(ManagedSessionState state) => new()
    {
        Version = 1,
        Id = state.Id,
        SolutionPath = state.SolutionPath,
        Description = state.Description,
        ConversationId = state.ConversationId,
        CreatedAt = state.CreatedAt,
        LastModifiedAt = state.LastModifiedAt,
        Baseline = state.BaselineCommitHash is not null
            ? new SessionBaseline
            {
                CommitHash = state.BaselineCommitHash,
                Timestamp = state.BaselineTimestamp ?? state.CreatedAt
            }
            : null,
        DocumentChanges = new Dictionary<string, string>(state.DocumentChanges, StringComparer.Ordinal),
        Snapshots = state.Snapshots.Select(s => new PersistedSnapshot
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatedAt = s.CreatedAt,
            DocumentChanges = new Dictionary<string, string>(s.DocumentChanges, StringComparer.Ordinal)
        }).ToList()
    };
}