using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>
/// Subtype configuration for Concurrency limit entries on MsSql connections.
/// Maps to <c>conn.MsSqlConcurrency</c>. is <c>conn.MsSqlConnectionLimit</c>.
///
/// Controls the maximum number of simultaneous in-flight queries against this
/// connection using a per-connection SemaphoreSlim. Requests that arrive when
/// the semaphore is at capacity receive a GenericResult.Failure instead of blocking.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "MsSql")]
public sealed partial class MsSqlConcurrencyConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid MsSqlConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum number of concurrent queries allowed.
    /// Requests that exceed this cap fail immediately with a ConcurrencyBlocked result.
    /// </summary>
    public int MaxConcurrentQueries { get; set; }
}
