using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Http.Limits;

/// <summary>
/// Subtype configuration for MaxRequestRate limit entries on Http connections.
/// Maps to <c>conn.HttpMaxRequestRate</c>. is <c>conn.HttpConnectionLimit</c>.
///
/// Controls outbound HTTP request rate using a token bucket per connection.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "Http")]
public sealed partial class HttpMaxRequestRateConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid HttpConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum number of HTTP requests per second.
    /// </summary>
    public int RequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the optional burst allowance above RequestsPerSecond for short spikes.
    /// When null, no burst headroom is granted.
    /// </summary>
    public int? BurstSize { get; set; }
}
