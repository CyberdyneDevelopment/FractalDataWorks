using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Http.Limits;

/// <summary>
/// Subtype configuration for MaxConcurrentRequests limit entries on Http connections.
/// Maps to <c>conn.HttpConcurrency</c>. is <c>conn.HttpConnectionLimit</c>.
///
/// Controls maximum simultaneous in-flight HTTP requests via a per-connection SemaphoreSlim.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "Http")]
public sealed partial class HttpConcurrencyConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid HttpConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum number of concurrent HTTP requests allowed.
    /// </summary>
    public int MaxConcurrent { get; set; }
}
