using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>
/// Subtype configuration for QueryTimeout limit entries on MsSql connections.
/// Maps to <c>conn.MsSqlQueryTimeout</c>. is <c>conn.MsSqlConnectionLimit</c>.
///
/// Wraps each outbound query in a linked CancellationTokenSource with this timeout.
/// The connection's own CommandTimeoutSeconds still applies as a separate SQL layer
/// timeout; this limit overrides at the FDW dispatch layer.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "MsSql")]
public sealed partial class MsSqlQueryTimeoutConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid MsSqlConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum time in seconds a single query may run before cancellation.
    /// </summary>
    public int TimeoutSeconds { get; set; }
}
