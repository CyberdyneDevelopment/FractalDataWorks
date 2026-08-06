using System;

namespace Fdw.Services.Connections.PostgreSql;

/// <summary>
/// KVP record for PostgreSqlConnectionAuthentication table.
/// </summary>
[Fdw.Data.GenerateMapper]
public sealed class PostgreSqlConnectionAuthKvp
{

    /// <summary>Gets or sets the parent FK.</summary>
    public Guid PostgreSqlConnectionId { get; set; }

    /// <summary>Gets or sets the key name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets whether this is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}
