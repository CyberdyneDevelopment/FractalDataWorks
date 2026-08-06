using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>
/// Subtype configuration for RateLimit entries on MsSql connections.
/// Maps to <c>conn.MsSqlRateLimit</c>. is <c>conn.MsSqlConnectionLimit</c>.
///
/// Controls outbound query throughput using a token-bucket model.
/// MaxPerSecond is required; MaxPerMinute and BurstSize are optional refinements.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "MsSql")]
public sealed partial class MsSqlRateLimitConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid MsSqlConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum number of queries per second across this connection.
    /// </summary>
    public int MaxPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the optional per-minute cap.
    /// When set, enforced in addition to MaxPerSecond.
    /// </summary>
    public int? MaxPerMinute { get; set; }

    /// <summary>
    /// Gets or sets the optional burst allowance above MaxPerSecond for short spikes.
    /// When null, no burst headroom is granted.
    /// </summary>
    public int? BurstSize { get; set; }
}
