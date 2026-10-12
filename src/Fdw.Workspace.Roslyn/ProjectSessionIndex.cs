#pragma warning disable MA0016 // Prefer using collection abstraction - DTOs need concrete types for JSON serialization

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents the session index stored in a project's .claude/roslyn.sessions file.
/// </summary>
/// <remarks>
/// <para>
/// This file is project-specific and can be committed to source control.
/// It provides a quick overview of sessions for a project without loading
/// full session data from the system store.
/// </para>
/// <para>
/// Multiple agents can share session awareness through this file, each
/// tracking their own conversation ID to identify their session.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class ProjectSessionIndex
{
    /// <summary>
    /// Gets or sets the schema version for forward compatibility.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the path to the project directory.
    /// </summary>
    [JsonPropertyName("projectPath")]
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the path to the solution file.
    /// </summary>
    [JsonPropertyName("solutionPath")]
    public string? SolutionPath { get; set; }

    /// <summary>
    /// Gets or sets the list of session entries.
    /// </summary>
    [JsonPropertyName("sessions")]
    public List<SessionIndexEntry> Sessions { get; set; } = [];

    /// <summary>
    /// Gets or sets the ID of the currently active session.
    /// </summary>
    [JsonPropertyName("activeSessionId")]
    public Guid? ActiveSessionId { get; set; }

    /// <summary>
    /// Gets or sets instructions for agents.
    /// </summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } =
        "Use ResumeSession(id) to continue work, or CreateSession() for new work. " +
        "Track your conversation ID with UpdateSession(conversationId: 'your-id').";
}