using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// A snapshot persisted to disk.
/// </summary>
public sealed class PersistedSnapshot
{
    /// <summary>
    /// Gets or sets the snapshot ID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the snapshot name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets when the snapshot was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the document changes at snapshot time.
    /// </summary>
    [JsonPropertyName("documentChanges")]
    public IDictionary<string, string> DocumentChanges { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}