using System;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// KVP record for the conn.HttpConnectionAuthentication table.
/// </summary>
[Fdw.Data.GenerateMapper]
public sealed class HttpConnectionAuthKvp
{

    /// <summary>Gets or sets the parent FK.</summary>
    public Guid HttpConnectionId { get; set; }

    /// <summary>Gets or sets the key name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets whether this is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}
