using System;
using System.Collections.Generic;
using Fdw.Services.Connections.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// SQL Server-specific implementation of IConnectionMetadata.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class MsSqlConnectionMetadata : IConnectionMetadata
{
    public string SystemName { get; init; } = string.Empty;
    public string? Version { get; init; }
    public string? ServerInfo { get; init; }
    public string? DatabaseName { get; init; }
    public IReadOnlyDictionary<string, object> Capabilities { get; init; } =
        new Dictionary<string, object>(StringComparer.Ordinal);
    public DateTimeOffset CollectedAt { get; init; }
    public IReadOnlyDictionary<string, object> CustomProperties { get; init; } =
        new Dictionary<string, object>(StringComparer.Ordinal);
}
