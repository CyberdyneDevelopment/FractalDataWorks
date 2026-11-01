using System;
using System.Text.Json.Serialization;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Baseline reference point for a session.
/// </summary>
public sealed class SessionBaseline
{
    /// <summary>
    /// Gets or sets the git commit hash at session start.
    /// </summary>
    [JsonPropertyName("commitHash")]
    public string? CommitHash { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the baseline.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}