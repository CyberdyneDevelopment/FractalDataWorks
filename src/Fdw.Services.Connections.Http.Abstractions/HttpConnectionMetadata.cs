using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Abstractions;

/// <summary>
/// Basic HTTP connection metadata implementation.
/// </summary>
internal sealed class HttpConnectionMetadata : IConnectionMetadata
{
    public HttpConnectionMetadata(string systemName)
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