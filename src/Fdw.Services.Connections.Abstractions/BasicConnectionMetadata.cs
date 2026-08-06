using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Basic connection metadata implementation.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class BasicConnectionMetadata : IConnectionMetadata
{
    public BasicConnectionMetadata(string systemName)
    {
        SystemName = systemName;
        CollectedAt = DateTimeOffset.UtcNow;
        Capabilities = new Dictionary<string, object>(StringComparer.Ordinal);
        CustomProperties = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    public string SystemName { get; }
    public string? Version => null;
    public string? ServerInfo => null;
    public string? DatabaseName => null;
    public IReadOnlyDictionary<string, object> Capabilities { get; }
    public DateTimeOffset CollectedAt { get; }
    public IReadOnlyDictionary<string, object> CustomProperties { get; }
}