using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// A lightweight session entry in the project index.
/// </summary>
/// <remarks>
/// Contains only metadata needed for session selection, not full session data.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class SessionIndexEntry
{
    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the session description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

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
    /// Gets or sets the conversation ID for Claude resume.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    /// <summary>
    /// Creates from session info.
    /// </summary>
    /// <param name="info">The session info.</param>
    /// <returns>An index entry.</returns>
    public static SessionIndexEntry FromSessionInfo(SessionInfo info) => new()
    {
        Id = info.Id,
        Description = info.Description,
        CreatedAt = info.CreatedAt,
        LastModifiedAt = info.LastModifiedAt,
        ConversationId = info.ConversationId
    };

    /// <summary>
    /// Creates from managed state.
    /// </summary>
    /// <param name="state">The managed session state.</param>
    /// <returns>An index entry.</returns>
    internal static SessionIndexEntry FromState(ManagedSessionState state) => new()
    {
        Id = state.Id,
        Description = state.Description,
        CreatedAt = state.CreatedAt,
        LastModifiedAt = state.LastModifiedAt,
        ConversationId = state.ConversationId
    };
}